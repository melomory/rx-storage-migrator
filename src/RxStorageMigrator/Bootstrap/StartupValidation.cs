using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RxStorageMigrator.Configuration;

namespace RxStorageMigrator.Bootstrap;

/// <summary>
/// Проверка корректности конфигурации и регистрации сервисов при запуске приложения.
/// </summary>
public static class StartupValidation
{
  /// <summary>
  /// Проверяет доступность и валидность обязательных настроек, зарегистрированных в контейнере зависимостей.
  /// </summary>
  /// <param name="services">Коллекция сервисов приложения.</param>
  /// <remarks>
  /// Метод создаёт временный <see cref="IServiceProvider"/> и принудительно
  /// запрашивает обязательные <see cref="IOptions{TOptions}"/>,
  /// чтобы убедиться, что конфигурация корректно привязана и валидирована.
  /// В случае ошибки будет выброшено исключение при запуске приложения.
  /// </remarks>
  public static void Validate(IServiceCollection services)
  {
    using var sp = services.BuildServiceProvider();

    _ = sp.GetRequiredService<IOptions<AppOptions>>().Value;
    _ = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
  }
}
