using RxStorageMigrator.Application.Models;

namespace RxStorageMigrator.Infrastructure.Data.Mappings;

/// <summary>
/// Содержит методы преобразования типа хранилища в код для базы данных и обратно.
/// </summary>
internal static class StorageTypeMapping
{
  /// <summary>
  /// Преобразовать <see cref="StorageType"/> в строковый код, используемый для хранения в базе данных.
  /// </summary>
  /// <param name="type">Тип хранилища.</param>
  /// <returns>
  /// Строковый код типа хранилища (<c>"S"</c> — SQL, <c>"F"</c> — файловое хранилище).
  /// </returns>
  /// <exception cref="ArgumentOutOfRangeException">Передан неподдерживаемый тип хранилища.</exception>
  internal static string ToDbCode(this StorageType type) =>
    type switch
    {
      StorageType.Sql => "S",
      StorageType.File => "F",
      _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

  /// <summary>
  /// Преобразовать строковый код из базы данных в значение <see cref="StorageType"/>.
  /// </summary>
  /// <param name="code">Строковый код типа хранилища.</param>
  /// <returns>Соответствующее значение <see cref="StorageType"/>.</returns>
  /// <exception cref="ArgumentException">Передан неизвестный код типа хранилища.</exception>
  internal static StorageType FromDbCode(this string code) =>
    code switch
    {
      "S" => StorageType.Sql,
      "F" => StorageType.File,
      _ => throw new ArgumentException($"Unknown storage type: {code}"),
    };
}
