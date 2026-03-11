namespace RxStorageMigrator.Infrastructure.Data.Sql;

/// <summary>
/// Коды ошибок SQL Server, используемые для определения типа сбоя.
/// </summary>
public static class SqlServerErrorCodes
{
  /// <summary>
  /// Тайм-аут выполнения команды.
  /// </summary>
  public const int Timeout = -2;

  /// <summary>
  /// Взаимная блокировка (deadlock victim).
  /// </summary>
  public const int Deadlock = 1205;

  /// <summary>
  /// Сброс соединения (connection reset).
  /// </summary>
  public const int ConnectionReset = 10054;

  /// <summary>
  /// Невозможно открыть базу данных.
  /// </summary>
  public const int CannotOpenDatabase = 4060;

  /// <summary>
  /// Ошибка обработки сервиса (Azure transient).
  /// </summary>
  public const int ServiceError = 40197;

  /// <summary>
  /// Throttling (Azure SQL).
  /// </summary>
  public const int Throttling = 40501;

  /// <summary>
  /// Набор transient-ошибок.
  /// </summary>
  public static readonly IReadOnlySet<int> TransientErrors =
    new HashSet<int>
    {
      Timeout,
      ConnectionReset,
      CannotOpenDatabase,
      ServiceError,
      Throttling,
      Deadlock,
    };
}
