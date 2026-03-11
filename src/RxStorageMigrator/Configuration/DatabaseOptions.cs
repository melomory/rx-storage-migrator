using System.ComponentModel.DataAnnotations;

namespace RxStorageMigrator.Configuration;

/// <summary>
/// Свойства конфигурации БД.
/// </summary>
public sealed class DatabaseOptions
{
  /// <summary>
  /// Строка подключения к БД Directum 5.
  /// </summary>
  /// <value>
  /// Строка подключения к SQL Server, используемая для доступа к базе Directum 5.
  /// </value>
  [Required]
  public string? SqlConnectionStringD5 { get; init; }
}
