using RxStorageMigrator.Application.Models;

namespace RxStorageMigrator.Infrastructure.Data.Mappings;

/// <summary>
/// Содержит методы преобразования файла миграции в результат.
/// </summary>
public static class MigrationResultMapping
{
  extension(MigrationFile migrationFile)
  {
    /// <summary>
    /// Преобразовать <see cref="MigrationFile"/> в <see cref="MigrationResult"/>
    /// с указанием статуса миграции и дополнительной информации.
    /// </summary>
    /// <param name="migrationStatus">Статус выполнения миграции.</param>
    /// <param name="comment">Комментарий или описание ошибки (если есть).</param>
    /// <param name="sourceFilePath">Путь к исходному файлу (если применимо).</param>
    /// <returns>Сформированный объект <see cref="MigrationResult"/>.</returns>
    public MigrationResult ToMigrationResult(
      MigrationStatus migrationStatus,
      string? comment,
      string? sourceFilePath)
    {
      return new(
        migrationFile.RowId,
        migrationFile.D5DocId,
        migrationFile.VersionNumber,
        migrationFile.RxDocId,
        migrationFile.BodyId,
        migrationStatus.ToDb(),
        comment,
        sourceFilePath,
        migrationFile.TargetFilePath);
    }

    /// <summary>
    /// Преобразовать <see cref="MigrationFile"/> в <see cref="MigrationResult"/>
    /// с указанием статуса миграции и дополнительной информации.
    /// </summary>
    /// <param name="migrationStatus">Статус выполнения миграции.</param>
    /// <returns>Сформированный объект <see cref="MigrationResult"/> без комментария и пути к исходному файлу.</returns>
    public MigrationResult ToMigrationResult(MigrationStatus migrationStatus)
    {
      return ToMigrationResult(migrationFile, migrationStatus, null, null);
    }

    /// <summary>
    /// Преобразовать <see cref="MigrationFile"/> в <see cref="MigrationResult"/>
    /// с указанием статуса миграции и дополнительной информации.
    /// </summary>
    /// <param name="migrationStatus">Статус выполнения миграции.</param>
    /// <param name="comment">Комментарий или описание ошибки (если есть).</param>
    /// <returns>Сформированный объект <see cref="MigrationResult"/> без указания пути к исходному файлу.</returns>
    public MigrationResult ToMigrationResult(MigrationStatus migrationStatus, string? comment)
    {
      return ToMigrationResult(migrationFile, migrationStatus, comment, null);
    }
  }
}
