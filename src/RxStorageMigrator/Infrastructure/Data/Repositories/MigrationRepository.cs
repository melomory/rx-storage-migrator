using System.Data;
using System.Runtime.CompilerServices;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RxStorageMigrator.Application.Interfaces;
using RxStorageMigrator.Application.Models;
using RxStorageMigrator.Application.Services;
using RxStorageMigrator.Configuration;
using RxStorageMigrator.Infrastructure.Data.Mappings;
using RxStorageMigrator.Infrastructure.Data.Queries;
using RxStorageMigrator.Infrastructure.Data.Sql;

namespace RxStorageMigrator.Infrastructure.Data.Repositories;

/// <summary>
/// Репозиторий миграции, обеспечивающий доступ к данным, формирование очереди миграции и сохранение результатов.
/// </summary>
/// <param name="logger">Логгер для записи диагностической информации.</param>
/// <param name="databaseOptions">Параметры подключения к базе данных.</param>
/// <param name="storageMetadataProvider">Провайдер метаданных хранилищ Directum 5 и Directum RX.</param>
public sealed class MigrationRepository(
  ILogger<MigrationService> logger,
  IOptions<DatabaseOptions> databaseOptions,
  IStorageMetadataProvider storageMetadataProvider) : IMigrationRepository
{
  /// <summary>
  /// Начальный размер пакета при сохранении результатов миграции.
  /// </summary>
  private const int InitialResultsBatchSize = 2_000;

  /// <summary>
  /// Минимально допустимый размер пакета при адаптивном уменьшении.
  /// </summary>
  private const int MinResultsBatchSize = 250;

  /// <summary>
  /// Максимально допустимый размер пакета при адаптивном увеличении.
  /// </summary>
  private const int MaxResultsBatchSize = 5_000;

  /// <summary>
  /// Шаг увеличения размера пакета при успешной записи.
  /// </summary>
  private const int ResultsBatchIncreaseStep = 250;

  /// <summary>
  /// Коэффициент уменьшения размера пакета при возникновении ошибок.
  /// </summary>
  /// <remarks>
  /// Текущий размер пакета делится на указанное значение.
  /// </remarks>
  private const int ResultsBatchReductionFactor = 2;

  /// <summary>
  /// Строка подключения к базе данных Directum 5.
  /// </summary>
  private readonly string _connectionString = databaseOptions.Value.SqlConnectionStringD5!;

  /// <inheritdoc />
  public Task<int> GetTotalMigratedDocumentsCountAsync(CancellationToken ct) =>
    WithConnectionAsync(
      connection => ExecuteCountAsync(
        connection,
        SqlQueries.SelectCountMigratedDocuments,
        ct),
      ct);

  /// <inheritdoc />
  public Task<int> GetTotalQueuedForTransferCountAsync(CancellationToken ct) =>
    WithConnectionAsync(
      connection => ExecuteCountAsync(
        connection,
        SqlQueries.SelectCountQueuedMigratedDocuments,
        ct),
      ct);

  /// <inheritdoc />
  public Task<int> GetQueuedForTransferCountAsync(CancellationToken ct) =>
    WithConnectionAsync(
      connection => ExecuteCountAsync(
        connection,
        SqlQueries.SelectCountQueuedMigratedDocumentsForTransfer,
        ct),
      ct);

  /// <inheritdoc />
  public Task<TransferredCountResult> GetTransferredDocumentsCountAsync(CancellationToken ct) =>
    WithConnectionAsync(
      connection => connection.QuerySingleAsync<TransferredCountResult>(
        new CommandDefinition(
          SqlQueries.SelectCountTransferredDocuments,
          cancellationToken: ct,
          commandTimeout: 0)),
      ct);

  /// <summary>
  /// Возвращает асинхронную последовательность файлов, подготовленных к миграции.
  /// </summary>
  /// <param name="batchSize">Максимальное количество записей, извлекаемых за один вызов.</param>
  /// <param name="ct">Токен отмены операции.</param>
  /// <returns>Асинхронная последовательность объектов <see cref="MigrationFile"/>.</returns>
  public async IAsyncEnumerable<MigrationFile> GetFilesAsync(
    int batchSize,
    [EnumeratorCancellation] CancellationToken ct)
  {
    logger.LogDebug("Fetching migration batch. BatchSize={BatchSize}", batchSize);

    await using var connection = new SqlConnection(_connectionString);

    await connection.OpenAsync(ct);

    var command = new CommandDefinition(
      SqlQueries.SelectMigratedDocumentsPaged,
      new
      {
        BatchSize = batchSize,
        MigrationStatus = MigrationStatus.InProgress.ToDb(),
      },
      cancellationToken: ct,
      commandTimeout: 0);

    var rows = await connection.QueryAsync<MigrationQueueRow>(command);
    var count = 0;

    foreach (var row in rows)
    {
      count++;

      yield return new MigrationFile(
        row.RowId,
        row.D5DocId,
        row.VersionNumber,
        row.BodyId,
        row.D5VerId,
        row.Extension,
        storageMetadataProvider.D5Storages[row.D5StorageId],
        row.RxDocId,
        storageMetadataProvider.RxStorages[row.RxStorageId]);
    }

    logger.LogDebug("Fetched {Count} files in current batch", count);
  }

  /// <summary>
  /// Выполняет пакетное сохранение результатов миграции с использованием табличного параметра (TVP).
  /// </summary>
  /// <param name="results">Результаты миграции для сохранения.</param>
  /// <param name="ct">Токен отмены операции.</param>
  /// <returns>Асинхронная задача выполнения операции.</returns>
  /// <remarks>
  /// <para>
  /// Для передачи данных используется <see cref="System.Data.DataTable"/>
  /// и табличный тип <c>dbo.MigrationFileResultTableType</c>.
  /// </para>
  /// <para>
  /// Сохранение выполняется через хранимую процедуру
  /// <c>dbo.SaveMigrationFileResults</c>.
  /// </para>
  /// <para>
  /// Если коллекция <paramref name="results"/> пуста,
  /// метод завершает выполнение без обращения к базе данных.
  /// </para>
  /// </remarks>
  /// <exception cref="OperationCanceledException">Отмена операции через <paramref name="ct"/>.</exception>
  /// <exception cref="SqlException">Ошибка подключения или выполнения хранимой процедуры.</exception>
  private async Task BulkSaveResultAsync(
    IReadOnlyCollection<MigrationResult> results,
    CancellationToken ct)
  {
    if (results.Count == 0)
      return;

    using var table = new DataTable();

    table.Columns.Add(nameof(MigrationResult.RowId), typeof(long));
    table.Columns.Add(nameof(MigrationResult.XRecId), typeof(int));
    table.Columns.Add(nameof(MigrationResult.Number), typeof(int));
    table.Columns.Add(nameof(MigrationResult.RxDocId), typeof(long));
    table.Columns.Add(nameof(MigrationResult.BodyId), typeof(Guid));
    table.Columns.Add(nameof(MigrationResult.MigrationStatus), typeof(string));
    table.Columns.Add(nameof(MigrationResult.Comment), typeof(string));
    table.Columns.Add(nameof(MigrationResult.D5FilePath), typeof(string));
    table.Columns.Add(nameof(MigrationResult.RxFilePath), typeof(string));

    foreach (var r in results)
    {
      table.Rows.Add(
        r.RowId,
        r.XRecId,
        r.Number,
        r.RxDocId,
        r.BodyId,
        r.MigrationStatus,
        r.Comment,
        r.D5FilePath,
        r.RxFilePath);
    }

    await using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync(ct);

    await connection.ExecuteAsync(
      "dbo.SaveMigrationFileResults",
      new
      {
        Items = table.AsTableValuedParameter(
          "dbo.MigrationFileResultTableType"),
      },
      commandType: System.Data.CommandType.StoredProcedure,
      commandTimeout: 60);
  }

  /// <inheritdoc />
  public async Task InitializeMigrationInfrastructureAsync(CancellationToken ct)
  {
    logger.LogInformation("Initializing migration infrastructure...");

    await using var connection = new SqlConnection(_connectionString);

    await connection.OpenAsync(ct);

    await using var transaction = await connection.BeginTransactionAsync(ct);

    try
    {
      logger.LogDebug("Creating migration queue table...");

      await connection.ExecuteAsync(
        new CommandDefinition(
          SqlQueries.CreateMigrationQueueTable,
          transaction: transaction,
          cancellationToken: ct,
          commandTimeout: 0));

      logger.LogDebug("Updating existing queue records before migration start...");

      await connection.ExecuteAsync(
        new CommandDefinition(
          SqlQueries.UpdateMigrationQueueBeforeStart,
          transaction: transaction,
          cancellationToken: ct,
          commandTimeout: 0));

      logger.LogDebug("Populating migration queue...");

      var inserted = await connection.ExecuteAsync(
        new CommandDefinition(
          SqlQueries.InsertMigrationQueue,
          transaction: transaction,
          cancellationToken: ct,
          commandTimeout: 0));

      logger.LogDebug("Ensuring migration result TVP type exists...");

      await connection.ExecuteAsync(
        new CommandDefinition(
          SqlQueries.CreateMigrationFileResultTableType,
          transaction: transaction,
          cancellationToken: ct,
          commandTimeout: 0));

      logger.LogDebug("Ensuring migration result stored procedure exists...");

      await connection.ExecuteAsync(
        new CommandDefinition(
          SqlQueries.CreateSaveMigrationResultsStoredProcedure,
          transaction: transaction,
          cancellationToken: ct,
          commandTimeout: 0));

      await transaction.CommitAsync(ct);

      logger.LogInformation(
        "Migration infrastructure initialized successfully. Queue entries inserted: {InsertedCount}",
        inserted);
    }
    catch
    {
      await transaction.RollbackAsync(ct);

      throw;
    }
  }

  /// <inheritdoc />
  public async Task SaveMigrationResultsAsync(IReadOnlyCollection<MigrationResult> results, CancellationToken ct)
  {
    var batchSize = InitialResultsBatchSize;

    var remaining = results.ToList();

    while (remaining.Count > 0)
    {
      var currentBatch = remaining
        .Take(batchSize)
        .ToList();

      try
      {
        await BulkSaveResultAsync(currentBatch, ct);
        remaining.RemoveRange(0, currentBatch.Count);

        if (batchSize < MaxResultsBatchSize)
          batchSize += ResultsBatchIncreaseStep;
      }
      catch (SqlException e) when (
        e.Number == SqlServerErrorCodes.Timeout ||
        e.Number == SqlServerErrorCodes.Deadlock)
      {
        batchSize /= ResultsBatchReductionFactor;

        if (batchSize < MinResultsBatchSize)
          throw;

        logger.LogInformation(e, "Reducing batch size to {BatchSize}", batchSize);
      }
    }
  }

  /// <summary>
  /// Выполнить асинхронную операцию с открытым подключением к базе данных.
  /// </summary>
  /// <typeparam name="T">Тип возвращаемого результата операции.</typeparam>
  /// <param name="action">
  /// Делегат, принимающий открытое <see cref="SqlConnection"/> и выполняющий требуемую операцию.
  /// </param>
  /// <param name="ct">Токен отмены операции.</param>
  /// <returns>Результат выполнения переданной функции.</returns>
  /// <remarks>
  /// Метод инкапсулирует создание и открытие подключения,
  /// гарантируя его корректное освобождение после выполнения операции.
  /// </remarks>
  /// <exception cref="OperationCanceledException">Отмена операции.</exception>
  /// <exception cref="SqlException">Ошибке открытия подключения или выполнения SQL‑операции.</exception>
  private async Task<T> WithConnectionAsync<T>(Func<SqlConnection, Task<T>> action, CancellationToken ct)
  {
    await using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync(ct);

    return await action(connection);
  }

  /// <summary>
  /// Выполнить SQL‑запрос, возвращающий одно числовое значение.
  /// </summary>
  /// <param name="connection">Открытое подключение к базе данных.</param>
  /// <param name="sql">SQL‑запрос для выполнения.</param>
  /// <param name="ct">Токен отмены операции.</param>
  /// <returns>
  /// Целочисленный результат запроса.
  /// Если результат равен <c>null</c>, возвращается <c>0</c>.
  /// </returns>
  /// <exception cref="SqlException">Ошибка выполнения SQL‑запроса.</exception>
  private async Task<int> ExecuteCountAsync(SqlConnection connection, string sql, CancellationToken ct)
  {
    var command = new CommandDefinition(
      sql,
      cancellationToken: ct,
      commandTimeout: 0);

    return await connection.ExecuteScalarAsync<int?>(command) ?? 0;
  }

  /// <summary>
  /// Асинхронно очищает таблицы миграции в базе данных.
  /// </summary>
  /// <param name="ct">Токен отмены операции.</param>
  /// <returns>Задача, представляющая асинхронную операцию очистки.</returns>
  /// <remarks>
  /// Очистка выполняется в рамках транзакции.
  /// В случае ошибки изменения откатываются.
  /// </remarks>
  /// <exception cref="OperationCanceledException">Отмена операции через <paramref name="ct"/>.</exception>
  /// <exception cref="SqlException">Ошибка выполнения SQL‑команд.</exception>
  public async Task ClearTablesAsync(CancellationToken ct)
  {
    await using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync(ct);
    await using var transaction = await connection.BeginTransactionAsync(ct);

    try
    {
      logger.LogDebug("Clearing migration queue table...");

      await connection.ExecuteAsync(
        new CommandDefinition(
          SqlQueries.TruncateMigrationQueueTable,
          transaction: transaction,
          cancellationToken: ct,
          commandTimeout: 0));

      logger.LogDebug("Clearing transferred documents table...");

      await connection.ExecuteAsync(
        new CommandDefinition(
          SqlQueries.TruncateTransferredTable,
          transaction: transaction,
          cancellationToken: ct,
          commandTimeout: 0));

      await transaction.CommitAsync(ct);
    }
    catch
    {
      await transaction.RollbackAsync(ct);

      throw;
    }
  }

  /// <summary>
  /// Представляет строку очереди миграции, полученную из базы данных.
  /// </summary>
  /// <param name="RowId">Идентификатор строки в таблице очереди.</param>
  /// <param name="D5DocId">Идентификатор документа в Directum 5.</param>
  /// <param name="VersionNumber">Номер версии документа в Directum 5.</param>
  /// <param name="BodyId">Идентификатор содержимого документа.</param>
  /// <param name="D5VerId">Идентификатор версии документа в Directum 5.</param>
  /// <param name="Extension">Расширение файла (без точки).</param>
  /// <param name="D5StorageId">Идентификатор исходного хранилища Directum 5.</param>
  /// <param name="RxDocId">Идентификатор документа в Directum RX.</param>
  /// <param name="RxStorageId">Идентификатор целевого хранилища Directum RX.</param>
  /// <remarks>
  /// Используется как DTO внутреннего слоя инфраструктуры для маппинга результатов SQL‑запроса.
  /// </remarks>
  private sealed record MigrationQueueRow(
    long RowId,
    int D5DocId,
    int VersionNumber,
    Guid BodyId,
    int D5VerId,
    string Extension,
    long D5StorageId,
    long RxDocId,
    long RxStorageId);
}
