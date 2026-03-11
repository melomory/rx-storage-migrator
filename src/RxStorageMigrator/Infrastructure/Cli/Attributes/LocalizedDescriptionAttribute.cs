using System.ComponentModel;
using System.Globalization;
using RxStorageMigrator.Infrastructure.Cli.Resources;

namespace RxStorageMigrator.Infrastructure.Cli.Attributes;

/// <summary>
/// Представляет атрибут <see cref="DescriptionAttribute"/>,
/// который получает локализованное описание из ресурсов
/// по указанному ключу.
/// </summary>
/// <remarks>
/// Использует <see cref="CliResources.ResourceManager"/> для получения
/// строки по ключу <see cref="Key"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class LocalizedDescriptionAttribute : DescriptionAttribute
{
  /// <summary>
  /// Инициализирует новый экземпляр <see cref="LocalizedDescriptionAttribute"/>
  /// с указанным ключом ресурса.
  /// </summary>
  /// <param name="key">Ключ строки в файле ресурсов.</param>
  public LocalizedDescriptionAttribute(string key)
    : base(key)
  {
    Key = key;
  }

  /// <summary>
  /// Ключ ресурса, используемый для получения локализованного описания.
  /// </summary>
  /// <value>
  /// Строковый ключ, соответствующий записи в файле ресурсов.
  /// </value>
  public string Key { get; }

  /// <summary>
  /// Возвращает локализованное описание из ресурсов.
  /// </summary>
  /// <value>
  /// Локализованная строка, полученная из ресурсов по ключу <see cref="Key"/>.
  /// </value>
  /// <exception cref="InvalidOperationException">
  /// Ресурс с указанным ключом не найден.
  /// </exception>
  public override string Description =>
    CliResources.ResourceManager.GetString(Key, CultureInfo.CurrentUICulture)
    ?? throw new InvalidOperationException($"Resource key '{Key}' was not found.");
}
