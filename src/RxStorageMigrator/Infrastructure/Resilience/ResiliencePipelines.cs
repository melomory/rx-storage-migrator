namespace RxStorageMigrator.Infrastructure.Resilience;

/// <summary>
/// Имена resilience-пайплайнов.
/// </summary>
public static class ResiliencePipelines
{
  /// <summary>
  /// Конвейер отказоустойчивости для операций SQL
  /// (Retry, Timeout и другие стратегии).
  /// </summary>
  public const string Sql = "sql-pipeline";
}
