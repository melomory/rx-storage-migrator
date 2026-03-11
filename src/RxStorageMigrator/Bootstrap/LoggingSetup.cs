using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RxStorageMigrator.Application.Interfaces;
using RxStorageMigrator.Infrastructure.Cli;
using Serilog;

namespace RxStorageMigrator.Bootstrap;

/// <summary>
/// Настройка логирования в CLI‑приложении.
/// </summary>
public static class LoggingSetup
{
  /// <summary>
  /// Зарегистрировать и настроить логгер для CLI‑приложения.
  /// </summary>
  /// <param name="services">Коллекция сервисов для регистрации зависимостей.</param>
  /// <param name="configuration">Конфигурация приложения.</param>
  /// <returns>Обновлённая коллекция <see cref="IServiceCollection"/>.</returns>
  public static IServiceCollection AddCliLogging(
    this IServiceCollection services,
    IConfiguration configuration)
  {
    var writeToSection = configuration
      .GetSection("Serilog:WriteTo")
      .GetChildren()
      .ToList();

    var hasConsoleSink = writeToSection
      .Exists(x => string.Equals(x["Name"], "Spectre", StringComparison.OrdinalIgnoreCase));

    services.AddSerilog(
      (serviceProviders, loggerConfiguration) =>
      {
        loggerConfiguration
          .ReadFrom.Configuration(configuration)
          .ReadFrom.Services(serviceProviders);

        var writeToFile = writeToSection
          .FirstOrDefault(x => x["Name"] == "File");

        if (writeToFile is null)
          return;

        var filePath = configuration["Args:path"];

        if (string.IsNullOrWhiteSpace(filePath))
          return;

        var normalizedPath = Path.IsPathRooted(filePath)
          ? filePath
          : Path.Combine(AppContext.BaseDirectory, filePath);

        loggerConfiguration.WriteTo.File(
          path: normalizedPath,
          formatProvider: CultureInfo.CurrentCulture);
      });

    if (hasConsoleSink)
      services.AddSingleton<IConsoleNotifier, NullConsoleNotifier>();
    else
      services.AddSingleton<IConsoleNotifier, SpectreConsoleNotifier>();

    return services;
  }
}
