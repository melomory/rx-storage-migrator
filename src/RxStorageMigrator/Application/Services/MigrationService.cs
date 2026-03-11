using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RxStorageMigrator.Application.Interfaces;
using RxStorageMigrator.Application.Models;
using RxStorageMigrator.Configuration;
using RxStorageMigrator.Infrastructure.Data.Mappings;
using RxStorageMigrator.Infrastructure.Security;
using Spectre.Console;

namespace RxStorageMigrator.Application.Services;

/// <summary>
/// Сервис, выполняющий миграцию файлов из исходного хранилища
/// в целевую систему с поддержкой пакетной и параллельной обработки.
/// </summary>
/// <param name="repository">
/// Репозиторий для получения данных о файлах и сохранения результатов миграции.
/// </param>
/// <param name="fileMigrator">
/// Компонент, выполняющий непосредственную миграцию одного файла.
/// </param>
/// <param name="appOptions">
/// Параметры приложения.
/// </param>
/// <param name="databaseOptions">
/// Параметры подключения к базе данных.
/// </param>
/// <param name="logger">
/// Логгер для записи диагностической информации.
/// </param>
/// <param name="consoleNotifier">
/// Компонент вывода уведомлений в консоль.
/// </param>
/// <param name="storageMetadataProvider">
/// Провайдер метаданных хранилища, инициализируемый перед началом миграции.
/// </param>
/// <remarks>
/// Управляет жизненным циклом миграции:
/// <list type="number">
/// <item><description>Инициализация метаданных и инфраструктуры миграции.</description></item>
/// <item><description>Получение общего количества документов.</description></item>
/// <item><description>Пакетная обработка файлов с ограничением параллелизма.</description></item>
/// <item><description>Сохранение результатов миграции.</description></item>
/// </list>
/// </remarks>
public sealed class MigrationService(
  IMigrationRepository repository,
  IFileMigrator fileMigrator,
  IOptions<AppOptions> appOptions,
  IOptions<DatabaseOptions> databaseOptions,
  ILogger<MigrationService> logger,
  IConsoleNotifier consoleNotifier,
  IStorageMetadataProvider storageMetadataProvider) : IMigrationService
{
  /// <summary>
  /// Максимальная степень параллелизма при обработке файлов.
  /// </summary>
  private readonly int _parallelism = appOptions.Value.MaxDegreeOfParallelism.GetValueOrDefault();

  /// <summary>
  /// Размер пакета файлов, извлекаемых за одну итерацию.
  /// </summary>
  private readonly int _batchSize = appOptions.Value.BatchSize.GetValueOrDefault();

  /// <summary>
  /// Количество обработанных файлов.
  /// </summary>
  private int _processed;

  /// <summary>
  /// Запустить процесс миграции.
  /// </summary>
  /// <param name="progress">
  /// Объект для уведомления о прогрессе выполнения.
  /// Кортеж содержит количество обработанных записей и общее количество к обработке.
  /// </param>
  /// <param name="ct">Токен отмены операции.</param>
  /// <returns>Задача, представляющая асинхронную операцию миграции.</returns>
  /// <exception cref="OperationCanceledException">Отмена операции через <paramref name="ct"/>.</exception>
  public async Task RunAsync(IProgress<(int Processed, int Total)> progress, CancellationToken ct)
  {
    logger.LogInformation("Starting migration service.");

    logger.LogInformation(
      "Configuration: Parallelism={Parallelism}, BatchSize={BatchSize}",
      _parallelism,
      _batchSize);

    logger.LogInformation(
      "Connection: {ConnectionString}",
      ConnectionStringMasker.Mask(databaseOptions.Value.SqlConnectionStringD5!));

    await storageMetadataProvider.InitializeAsync(ct);

    var totalDocuments = await repository.GetTotalMigratedDocumentsCountAsync(ct);

    logger.LogInformation("Migrated documents total: {TotalDocuments}", totalDocuments);
    consoleNotifier.WriteInfo($"Migrated documents total: {totalDocuments}");

    await repository.InitializeMigrationInfrastructureAsync(ct);

    var queuedTotal = await repository.GetTotalQueuedForTransferCountAsync(ct);

    logger.LogInformation("Queued migrated documents total: {QueuedTotal}", queuedTotal);
    consoleNotifier.WriteInfo($"Queued migrated documents total: {queuedTotal}");

    var queuedForTransferCount = await repository.GetQueuedForTransferCountAsync(ct);

    logger.LogInformation("Queued migrated documents for transfer: {QueuedForTransferCount}", queuedForTransferCount);
    consoleNotifier.WriteInfo($"Queued migrated documents for transfer: {queuedForTransferCount}");

    if (queuedForTransferCount == 0)
    {
      progress.Report((0, 0));
      logger.LogInformation("Nothing to migrate.");
      consoleNotifier.WriteInfo("Nothing to migrate.");

      return;
    }

    using var semaphore = new SemaphoreSlim(_parallelism);
    List<MigrationFile> files;

    do
    {
      files = await repository.GetFilesAsync(_batchSize, ct).ToListAsync(ct);

      if (files.Count == 0)
        continue;

      var tasks = files
        .Select(
          file => ProcessWithSemaphoreAsync(
            file,
            semaphore,
            queuedForTransferCount,
            progress,
            ct))
        .ToArray();

      var results = await Task.WhenAll(tasks);

      await repository.SaveMigrationResultsAsync(results.ToList(), ct);
    }
    while (files.Count > 0 && !ct.IsCancellationRequested);

    logger.LogInformation(
      "Migration completed. {Processed}/{QueuedForTransferCount}",
      _processed,
      queuedForTransferCount);

    consoleNotifier.WriteInfo($"Migration completed. {_processed}/{queuedForTransferCount}");

    var transferredCountResult = await repository.GetTransferredDocumentsCountAsync(ct);

    logger.LogInformation(
      "Transferred documents: {PerformedCount}/{TotalCount}",
      transferredCountResult.PerformedCount,
      transferredCountResult.TotalCount);

    consoleNotifier.WriteInfo(
      $"Transferred documents: {transferredCountResult.PerformedCount}/{transferredCountResult.TotalCount}");
  }

  /// <summary>
  /// Выполнить обработку файла с использованием семафора для ограничения степени параллелизма.
  /// </summary>
  /// <param name="file">Файл миграции для обработки.</param>
  /// <param name="semaphore">Семафор, ограничивающий количество одновременно выполняемых задач.</param>
  /// <param name="total">Общее количество файлов для обработки.</param>
  /// <param name="progress">Объект для уведомления о прогрессе выполнения.</param>
  /// <param name="ct">Токен отмены операции.</param>
  /// <returns>Результат обработки файла в виде <see cref="MigrationResult"/>.</returns>
  /// <exception cref="OperationCanceledException">Отмена операции через <paramref name="ct"/>.</exception>
  private async Task<MigrationResult> ProcessWithSemaphoreAsync(
    MigrationFile file,
    SemaphoreSlim semaphore,
    int total,
    IProgress<(int Processed, int Total)> progress,
    CancellationToken ct)
  {
    await semaphore.WaitAsync(ct);

    try
    {
      return await ProcessFileAsync(file, total, progress, ct);
    }
    finally
    {
      semaphore.Release();
    }
  }

  /// <summary>
  /// Выполнить миграцию файла.
  /// </summary>
  /// <param name="file">Файл миграции для обработки.</param>
  /// <param name="total">Общее количество файлов для обработки.</param>
  /// <param name="progress">Объект для уведомления о прогрессе выполнения.</param>
  /// <param name="ct">Токен отмены операции.</param>
  /// <returns>
  /// Результат миграции файла.
  /// В случае ошибки возвращается результат со статусом <see cref="MigrationStatus.Error"/>.
  /// </returns>
  private async Task<MigrationResult> ProcessFileAsync(
    MigrationFile file,
    int total,
    IProgress<(int Processed, int Total)> progress,
    CancellationToken ct)
  {
    try
    {
      var result = await fileMigrator.MigrateAsync(file, ct);

      return result.Value!;
    }
    catch (Exception e)
    {
      return file.ToMigrationResult(MigrationStatus.Error, e.Message);
    }
    finally
    {
      ReportProgress(total, progress);
    }
  }

  /// <summary>
  /// Уведомить о прогрессе.
  /// </summary>
  /// <param name="total">Общее количество файлов для обработки.</param>
  /// <param name="progress">Объект уведомления о прогрессе.</param>
  /// <remarks>
  /// Метод потокобезопасен благодаря использованию <see cref="Interlocked"/>.
  /// Логирование выполняется каждые <c>_batchSize</c> обработанных файлов.
  /// </remarks>
  private void ReportProgress(int total, IProgress<(int Processed, int Total)> progress)
  {
    var current = Interlocked.Increment(ref _processed);

    if (current % _batchSize == 0)
    {
      logger.LogInformation("Processed {Processed}/{Total}", current, total);
      consoleNotifier.WriteInfo($"Processed {current}/{total}");
    }

    progress.Report((current, total));
  }

  /// <summary>
  /// Выполняет очистку таблиц миграции.
  /// </summary>
  /// <param name="ct">Токен отмены операции.</param>
  /// <returns>Асинхронная задача выполнения операции очистки.</returns>
  /// <exception cref="OperationCanceledException">Отмена операции через <paramref name="ct"/>.</exception>
  public async Task ClearAsync(CancellationToken ct)
  {
    logger.LogInformation("Clearing migration tables...");

    await repository.ClearTablesAsync(ct);

    logger.LogInformation("Tables cleared.");
  }
}
