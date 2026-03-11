namespace RxStorageMigrator.Application.Models;

/// <summary>
/// Тип хранилища документа.
/// </summary>
public enum StorageType
{
  /// <summary>
  /// SQL‑хранилище.
  /// </summary>
  /// <remarks> Соответствует коду "S" в базе данных.</remarks>
  Sql,

  /// <summary>
  /// Файловое хранилище.
  /// </summary>
  /// <remarks>Соответствует коду "F" в базе данных.</remarks>
  File,
}
