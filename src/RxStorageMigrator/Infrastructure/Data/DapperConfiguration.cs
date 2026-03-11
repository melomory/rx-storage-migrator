using Dapper;
using RxStorageMigrator.Infrastructure.Data.Mappings;

namespace RxStorageMigrator.Infrastructure.Data;

/// <summary>
/// Конфигурация Dapper для приложения.
/// </summary>
internal static class DapperConfiguration
{
  /// <summary>
  /// Признак, что инициализация выполнена.
  /// </summary>
  private static bool _initialized;

  /// <summary>
  /// Регистрирует пользовательские обработчики типов и выполняет однократную инициализацию Dapper.
  /// </summary>
  /// <remarks>
  /// Метод безопасен для повторного вызова — конфигурация выполняется только один раз.
  /// </remarks>
  internal static void Configure()
  {
    if (_initialized)
      return;

    SqlMapper.AddTypeHandler(new StorageTypeHandler());

    _initialized = true;
  }
}
