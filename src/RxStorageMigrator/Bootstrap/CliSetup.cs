using Microsoft.Extensions.DependencyInjection;
using RxStorageMigrator.Commands;
using RxStorageMigrator.Infrastructure.Cli;
using Spectre.Console.Cli;

namespace RxStorageMigrator.Bootstrap;

/// <summary>
/// Настройка и создание CLI‑приложения.
/// </summary>
public static class CliSetup
{
  /// <summary>
  /// Создать и настроить экземпляр <see cref="CommandApp{TCommand}"/>
  /// с использованием переданной коллекции сервисов.
  /// </summary>
  /// <param name="services">Коллекция сервисов, используемая для регистрации зависимостей
  /// и создания <see cref="TypeRegistrar"/>.
  /// </param>
  /// <returns>Настроенный экземпляр <see cref="CommandApp{RunCommand}"/>.</returns>
  public static CommandApp<RunCommand> Create(IServiceCollection services)
  {
    var registrar = new TypeRegistrar(services);
    var app = new CommandApp<RunCommand>(registrar);

    app.Configure(
      config =>
      {
        config.SetApplicationName(nameof(RxStorageMigrator));
#if DEBUG
        config.PropagateExceptions();
        config.ValidateExamples();
#endif
      });

    return app;
  }
}
