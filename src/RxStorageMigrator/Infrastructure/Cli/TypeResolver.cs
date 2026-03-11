using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace RxStorageMigrator.Infrastructure.Cli;

/// <summary>
/// Реализация <see cref="ITypeResolver"/> для разрешения зависимостей
/// через <see cref="ServiceProvider"/> Microsoft.Extensions.DependencyInjection.
/// </summary>
/// <param name="provider">Провайдер сервисов, используемый для разрешения зависимостей.</param>
public sealed class TypeResolver(ServiceProvider provider) : ITypeResolver, IDisposable
{
  /// <inheritdoc />
  public object? Resolve(Type? type) => type == null ? null : provider.GetService(type);

  /// <inheritdoc />
  public void Dispose() => provider.Dispose();
}
