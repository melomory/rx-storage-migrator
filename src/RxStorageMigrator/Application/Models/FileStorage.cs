using RxStorageMigrator.Infrastructure.Data.Mappings;

namespace RxStorageMigrator.Application.Models;

/// <summary>
/// Файловое хранилище.
/// </summary>
/// <param name="Id">Идентификатор хранилища.</param>
/// <param name="Name">Имя хранилища.</param>
/// <param name="StorageTypeCode">Код типа хранилища.</param>
/// <param name="Path">Путь расположения хранилища.</param>
public record FileStorage(
  long Id,
  string Name,
  string StorageTypeCode,
  string Path)
{
  /// <summary>
  /// Тип хранилища, преобразованный из кода, полученного из базы данных.
  /// </summary>
  /// <value>Значение <see cref="StorageType"/>, преобразованное из <c>StorageTypeCode</c>.</value>
  public StorageType StorageType => StorageTypeCode.FromDbCode();
}
