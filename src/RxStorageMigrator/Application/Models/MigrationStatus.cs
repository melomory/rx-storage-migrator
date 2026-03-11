namespace RxStorageMigrator.Application.Models;

/// <summary>
/// Статус миграции.
/// </summary>
public enum MigrationStatus
{
  /// <summary>
  /// В процессе миграции.
  /// </summary>
  InProgress,

  /// <summary>
  /// Миграция выполнена успешно.
  /// </summary>
  Performed,

  /// <summary>
  /// В процессе миграции произошла ошибка.
  /// </summary>
  Error,
}
