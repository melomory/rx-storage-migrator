using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RxStorageMigrator.Application.Interfaces;
using RxStorageMigrator.Application.Services;
using RxStorageMigrator.Configuration;
using RxStorageMigrator.Infrastructure.Data;
using RxStorageMigrator.Infrastructure.Data.Repositories;
using RxStorageMigrator.Infrastructure.Data.Sql;
using RxStorageMigrator.Infrastructure.Resilience;
using RxStorageMigrator.Infrastructure.Storage;

namespace RxStorageMigrator.Bootstrap;

/// <summary>
/// Содержит методы расширения для регистрации сервисов приложения
/// и инфраструктурного слоя в контейнере зависимостей.
/// </summary>
public static class ServiceCollectionExtensions
{
  /// <summary>
  /// Расширения для <see cref="IServiceCollection"/>.
  /// </summary>
  extension(IServiceCollection services)
  {
    /// <summary>
    /// Зарегистрировать сервисы слоя Application.
    /// </summary>
    /// <returns>Обновлённая коллекция сервисов.</returns>
    public IServiceCollection AddApplication()
    {
      services.AddScoped<IMigrationService, MigrationService>();

      return services;
    }

    /// <summary>
    /// Зарегистрировать сервисы инфраструктурного слоя,
    /// включая доступ к данным, конфигурацию Dapper,
    /// логирование и параметры приложения.
    /// </summary>
    /// <param name="config">Конфигурация приложения.</param>
    /// <returns>Обновлённая коллекция сервисов.</returns>
    public IServiceCollection AddInfrastructure(IConfiguration config)
    {
      DapperConfiguration.Configure();

      services.AddOptionsWithValidateOnStart<AppOptions>()
        .BindConfiguration("App")
        .ValidateDataAnnotations();

      services.AddOptionsWithValidateOnStart<DatabaseOptions>()
        .BindConfiguration("ConnectionStrings")
        .ValidateDataAnnotations();

      services.AddCliLogging(config);

      services.AddSingleton<IStorageMetadataProvider, StorageMetadataProvider>();
      services.AddScoped<IMigrationRepository, MigrationRepository>();
      services.AddScoped<IFileMigrator, FileMigrator>();

      services.AddResiliencePipeline(
        ResiliencePipelines.Sql,
        (builder, context) =>
        {
          var options = context.ServiceProvider
            .GetRequiredService<IOptions<AppOptions>>()
            .Value;

          var logger = context.ServiceProvider.GetRequiredService<ILogger<SqlResiliencePipeline>>();

          builder.AddRetry(
            new RetryStrategyOptions
            {
              MaxRetryAttempts = options.Resilience.SqlMaxRetryAttempts,
              BackoffType = DelayBackoffType.Exponential,
              UseJitter = true,
              ShouldHandle = new PredicateBuilder()
                .Handle<SqlException>(
                  ex =>
                    SqlServerErrorCodes.TransientErrors.Contains(ex.Number))
                .Handle<IOException>(),
              OnRetry = args =>
              {
                logger.LogWarning(
                  args.Outcome.Exception,
                  "Retry {Attempt}",
                  args.AttemptNumber);

                return default;
              },
            });
        });

      return services;
    }
  }
}
