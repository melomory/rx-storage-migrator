using RxStorageMigrator.Application.Models;

namespace RxStorageMigrator.Infrastructure.Data.Mappings;

/// <summary>
/// Содержит методы преобразования статуса миграции в формат базы данных и обратно.
/// </summary>
public static class MigrationStatusMapping
{
  /// <summary>
  /// Преобразует <see cref="MigrationStatus"/> в строковое представление, используемое для хранения в базе данных.
  /// </summary>
  /// <param name="status">Статус миграции.</param>
  /// <returns>Строковое представление статуса.</returns>
  public static string ToDb(this MigrationStatus status) => status.ToString();

  /// <summary>
  /// Преобразует строковое значение из базы данных в <see cref="MigrationStatus"/>.
  /// </summary>
  /// <param name="value">Строковое представление статуса.</param>
  /// <returns>Соответствующее значение <see cref="MigrationStatus"/>.</returns>
  /// <exception cref="ArgumentException">Значение не соответствует ни одному элементу перечисления.</exception>
  public static MigrationStatus FromDb(string value) => Enum.Parse<MigrationStatus>(value);
}
