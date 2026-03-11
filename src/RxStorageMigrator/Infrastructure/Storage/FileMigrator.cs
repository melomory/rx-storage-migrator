using System.Data;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Registry;
using RxStorageMigrator.Application.Common;
using RxStorageMigrator.Application.Interfaces;
using RxStorageMigrator.Application.Models;
using RxStorageMigrator.Application.Services;
using RxStorageMigrator.Configuration;
using RxStorageMigrator.Infrastructure.Data.Mappings;
using RxStorageMigrator.Infrastructure.Data.Queries;
using RxStorageMigrator.Infrastructure.Resilience;

namespace RxStorageMigrator.Infrastructure.Storage;

/// <summary>
/// Выполняет миграцию содержимого документа из исходного хранилища в целевое.
/// </summary>
/// <remarks>
/// Поддерживаются два типа источника:
/// <list type="bullet">
/// <item><description>SQL‑хранилище (чтение BLOB из базы данных).</description></item>
/// <item><description>Файловое хранилище (копирование файла).</description></item>
/// </list>
/// Реализует retry‑логику для временных ошибок и ограничение параллельных чтений из базы данных.
/// </remarks>
/// <param name="pipelineProvider">Провайдер конвейера отказоустойчивости.</param>
/// <param name="databaseOptions">Параметры подключения к БД.</param>
/// <param name="appOptions">Параметры приложения.</param>
/// <param name="logger">Логгер.</param>
public sealed class FileMigrator(
  ResiliencePipelineProvider<string> pipelineProvider,
  IOptions<DatabaseOptions> databaseOptions,
  IOptions<AppOptions> appOptions,
  ILogger<MigrationService> logger) : IFileMigrator, IDisposable
{
  /// <summary>
  /// Тайм-аут выполнения SQL‑команды в секундах.
  /// </summary>
  private const int DbCommandTimeoutSeconds = 600;

  /// <summary>
  /// Размер буфера потоков при копировании файлов.
  /// </summary>
  private const int DefaultStreamBufferSize = 81920;

  /// <summary>
  /// Строка подключения к базе данных Directum 5.
  /// </summary>
  private readonly string _connectionString = databaseOptions.Value.SqlConnectionStringD5!;

  /// <summary>
  /// Ограничитель параллельных операций чтения из базы данных.
  /// </summary>
  private readonly SemaphoreSlim _dbReadLimiter = new(appOptions.Value.MaxParallelDbReads);

  /// <summary>
  /// Конвейер отказоустойчивости для операций работы с SQL,
  /// включающий стратегии Retry, Timeout и другие механизмы,
  /// зарегистрированные под именем <see cref="ResiliencePipelines.Sql"/>.
  /// </summary>
  private readonly ResiliencePipeline _pipeline = pipelineProvider.GetPipeline(ResiliencePipelines.Sql);

  /// <summary>
  /// Признак того, что экземпляр уже был освобождён.
  /// </summary>
  /// <remarks>
  /// Используется для предотвращения повторного вызова <see cref="Dispose()"/>.
  /// </remarks>
  private bool _disposed;

  /// <summary>
  /// Выполнить миграцию одного файла.
  /// </summary>
  /// <param name="file">Файл для миграции.</param>
  /// <param name="ct">Токен отмены.</param>
  /// <returns>
  /// Результат операции в виде <see cref="Result{MigrationResult}"/>.
  /// </returns>
  /// <exception cref="OperationCanceledException">Отмена операции.</exception>
  public async Task<Result<MigrationResult>> MigrateAsync(MigrationFile file, CancellationToken ct)
  {
    ArgumentNullException.ThrowIfNull(file);

    try
    {
      Directory.CreateDirectory(file.GetTargetFolder());

      return file.SourceFileStorage.StorageType switch
      {
        StorageType.Sql => await MigrateFromDatabaseAsync(file, ct),
        StorageType.File => await MigrateBodyFromFsToFsAsync(file, ct),
        _ => Result<MigrationResult>.Failure(
          file.ToMigrationResult(
            MigrationStatus.Error,
            $"Unsupported storage type {file.SourceFileStorage.StorageType}"),
          "Unsupported storage type"),
      };
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (Exception ex)
    {
      CleanupPartialFile(file.TargetFilePath);

      var message = $"Migration failed for {file.D5DocId} v{file.VersionNumber}";

      logger.LogError(
        ex,
        message);

      return Result<MigrationResult>.Failure(file.ToMigrationResult(MigrationStatus.Error, message), message);
    }
  }

  /// <summary>
  /// Выполнить миграцию содержимого из SQL‑хранилища.
  /// </summary>
  /// <param name="file">Файл миграции.</param>
  /// <param name="ct">Токен отмены.</param>
  /// <returns>Результат миграции.</returns>
  /// <remarks>
  /// Использует потоковое чтение BLOB через
  /// <see cref="System.Data.CommandBehavior.SequentialAccess"/>.
  /// Реализует retry‑логику при временных SQL‑ошибках и IO‑ошибках.
  /// </remarks>
  private async Task<Result<MigrationResult>> MigrateFromDatabaseAsync(
    MigrationFile file,
    CancellationToken ct)
  {
    var startedAt = Stopwatch.GetTimestamp();

    try
    {
      return await _pipeline.ExecuteAsync(
        async token =>
        {
          await _dbReadLimiter.WaitAsync(token);

          try
          {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            await using var command = new SqlCommand(
              SqlQueries.SelectFileVersion,
              connection) { CommandTimeout = DbCommandTimeoutSeconds };

            command.Parameters.Add(new SqlParameter("@VersionId", SqlDbType.BigInt) { Value = file.VersionId });

            await using var reader = await command.ExecuteReaderAsync(
              CommandBehavior.SequentialAccess |
              CommandBehavior.SingleRow,
              token);

            if (!await reader.ReadAsync(token))
            {
              return Result<MigrationResult>.Failure(
                file.ToMigrationResult(
                  MigrationStatus.Error,
                  $"File version not found. VersionId={file.VersionId}"),
                "File version not found");
            }

            await using var databaseStream = reader.GetStream(0);

            var directory = Path.GetDirectoryName(file.TargetFilePath)!;
            Directory.CreateDirectory(directory);

            await using var fileStream = new FileStream(
              file.TargetFilePath,
              FileMode.Create,
              FileAccess.Write,
              FileShare.None,
              DefaultStreamBufferSize,
              useAsync: true);

            await databaseStream.CopyToAsync(fileStream, DefaultStreamBufferSize, token);
          }
          finally
          {
            _dbReadLimiter.Release();
          }

          var elapsedMs = Stopwatch
            .GetElapsedTime(startedAt)
            .TotalMilliseconds;

          logger.LogDebug(
            "Migration completed successfully. VersionId={VersionId}, ElapsedMs={ElapsedMs}",
            file.VersionId,
            elapsedMs);

          return Result<MigrationResult>.Success(
            file.ToMigrationResult(MigrationStatus.Performed));
        },
        ct);
    }
    catch (Exception ex)
    {
      CleanupPartialFile(file.TargetFilePath);

      var message =
        $"Document migration error (Id - {file.D5DocId}, version - {file.VersionNumber}), " +
        $"TargetPath={file.TargetFilePath}: {ex.Message}";

      logger.LogError(ex, message);

      return Result<MigrationResult>.Failure(
        file.ToMigrationResult(MigrationStatus.Error, message),
        message);
    }
  }

  /// <summary>
  /// Удаляет частично записанный файл при ошибке.
  /// </summary>
  /// <param name="path">Путь к файлу.</param>
  /// <remarks>
  /// Исключения игнорируются, так как ошибка уже была залогирована.
  /// </remarks>
  private static void CleanupPartialFile(string path)
  {
    try
    {
      if (File.Exists(path))
        File.Delete(path);
    }
    catch
    {
      // Игнорируем — лог уже есть.
    }
  }

  /// <summary>
  /// Выполнить миграцию файла из файловой системы в файловую систему.
  /// </summary>
  /// <param name="file">Файл миграции.</param>
  /// <param name="ct">Токен отмены.</param>
  /// <returns>Результат миграции.</returns>
  /// <remarks>
  /// <para>
  /// Поиск исходного файла выполняется по шаблону:
  /// <c>({D5DocId} v{VersionNumber}).{Extension}</c>.
  /// </para>
  /// <para>
  /// Копирование осуществляется асинхронно с использованием буферизированных потоков.
  /// </para>
  /// </remarks>
  /// <exception cref="OperationCanceledException">Отмена операции.</exception>
  private async Task<Result<MigrationResult>> MigrateBodyFromFsToFsAsync(MigrationFile file, CancellationToken ct)
  {
    logger.LogDebug("Starting file migration. Id={Id}, Version={Version}", file.D5DocId, file.VersionNumber);

    try
    {
      ct.ThrowIfCancellationRequested();

      var sourceFolder = file.SourceFolderPath;

      if (!Directory.Exists(sourceFolder))
      {
        logger.LogDebug(
          "Source folder not found. Id={Id}, Version={Version}, Path={Path}",
          file.D5DocId,
          file.VersionNumber,
          sourceFolder);

        return Result<MigrationResult>.Failure(
          file.ToMigrationResult(MigrationStatus.Error, $"Source folder not found: {sourceFolder}"),
          $"Source folder not found: {sourceFolder}");
      }

      var sourceFilePath = Directory
        .EnumerateFiles(sourceFolder, "*.*", SearchOption.TopDirectoryOnly)
        .FirstOrDefault(
          x =>
            x.EndsWith(
              $"({file.D5DocId} v{file.VersionNumber}).{file.Extension}",
              StringComparison.OrdinalIgnoreCase) &&
            !x.Contains("~$"));

      if (string.IsNullOrWhiteSpace(sourceFilePath))
      {
        sourceFilePath = Directory.EnumerateFiles(sourceFolder, "*.*")
          .FirstOrDefault(
            d =>
              Path.GetFileNameWithoutExtension(d)
                .EndsWith($"({file.D5DocId} v{file.VersionNumber})", StringComparison.OrdinalIgnoreCase));
      }

      if (string.IsNullOrWhiteSpace(sourceFilePath))
      {
        logger.LogDebug(
          "Source file not found. Id={Id}, Version={Version}, Folder={Folder}",
          file.D5DocId,
          file.VersionNumber,
          sourceFolder);

        return Result<MigrationResult>.Failure(
          file.ToMigrationResult(
            MigrationStatus.Error,
            $"File ({file.D5DocId} v{file.VersionNumber}) not found in {sourceFolder}"),
          $"File ({file.D5DocId} v{file.VersionNumber}) not found in {sourceFolder}");
      }

      Directory.CreateDirectory(file.GetTargetFolder());

      logger.LogDebug(
        "Copying file from {SourcePath} to {TargetPath}",
        sourceFilePath,
        file.TargetFilePath);

      await using var sourceStream = new FileStream(
        sourceFilePath,
        FileMode.Open,
        FileAccess.Read,
        FileShare.Read,
        bufferSize: DefaultStreamBufferSize,
        useAsync: true);

      await using var targetStream = new FileStream(
        file.TargetFilePath,
        FileMode.Create,
        FileAccess.Write,
        FileShare.None,
        bufferSize: DefaultStreamBufferSize,
        useAsync: true);

      await sourceStream.CopyToAsync(targetStream, ct);

      logger.LogDebug(
        "File migrated successfully. Id={Id}, Version={Version}",
        file.D5DocId,
        file.VersionNumber);

      return Result<MigrationResult>.Success(file.ToMigrationResult(MigrationStatus.Performed, null, sourceFilePath));
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (Exception e)
    {
      var message =
        $"Document migration error (Id - {file.D5DocId}, version - {file.VersionNumber}): {e.Message}";

      logger.LogError(e, message);

      return Result<MigrationResult>.Failure(file.ToMigrationResult(MigrationStatus.Error, message), message);
    }
  }

  /// <inheritdoc />
  public void Dispose()
  {
    if (_disposed)
      return;

    _dbReadLimiter.Dispose();
    _disposed = true;
  }
}
