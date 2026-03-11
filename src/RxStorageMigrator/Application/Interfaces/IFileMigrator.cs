using RxStorageMigrator.Application.Common;
using RxStorageMigrator.Application.Models;

namespace RxStorageMigrator.Application.Interfaces;

/// <summary>
/// Контракт для сервиса миграции файлов.
/// </summary>
public interface IFileMigrator
{
  /// <summary>
  /// Выполнить миграцию указанного файла.
  /// </summary>
  /// <param name="file">Информация о файле, подлежащем миграции.</param>
  /// <param name="ct">Токен отмены, позволяющий прервать выполнение операции.</param>
  /// <returns>Результат выполнения миграции в виде <see cref="Result{MigrationResult}"/>.</returns>
  Task<Result<MigrationResult>> MigrateAsync(MigrationFile file, CancellationToken ct);
}
