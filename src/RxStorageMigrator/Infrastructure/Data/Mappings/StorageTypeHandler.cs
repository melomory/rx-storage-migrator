using System.Data;
using Dapper;
using RxStorageMigrator.Application.Models;

namespace RxStorageMigrator.Infrastructure.Data.Mappings;

/// <summary>
/// Обработчик типа для Dapper, обеспечивающий преобразование
/// между <see cref="StorageType"/> и его представлением в базе данных.
/// </summary>
/// <remarks>
/// Используется для автоматического маппинга строкового кода типа хранилища
/// в <see cref="StorageType"/> и обратно.
/// </remarks>
public sealed class StorageTypeHandler : SqlMapper.TypeHandler<StorageType>
{
  /// <summary>
  /// Преобразовать значение из базы данных в <see cref="StorageType"/>.
  /// </summary>
  /// <param name="value">Значение, полученное из базы данных (ожидается строковый код).</param>
  /// <returns>Соответствующий <see cref="StorageType"/>.</returns>
  /// <exception cref="DataException">
  /// Выбрасывается, если значение не может быть преобразовано в <see cref="StorageType"/>.
  /// </exception>
  public override StorageType Parse(object value)
  {
    if (value is null)
      throw new DataException("Cannot convert null to StorageType.");

    return value switch
    {
      string s => s.FromDbCode(),
      _ => throw new DataException($"Cannot convert {value.GetType()} to StorageType"),
    };
  }

  /// <inheritdoc/>
  public override void SetValue(IDbDataParameter parameter, StorageType value)
  {
    parameter.Value = value.ToDbCode();
    parameter.DbType = DbType.String;
  }
}
