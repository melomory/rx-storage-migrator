using System.ComponentModel.DataAnnotations;

namespace RxStorageMigrator.Configuration;

/// <summary>
/// Параметры конфигурации приложения.
/// </summary>
public sealed class AppOptions
{
  /// <summary>
  /// Минимально допустимая степень параллелизма миграции.
  /// </summary>
  private const int MinParallelism = 1;

  /// <summary>
  /// Максимально допустимая степень параллелизма миграции.
  /// </summary>
  private const int MaxParallelism = 256;

  /// <summary>
  /// Минимально допустимое количество параллельных операций чтения из БД.
  /// </summary>
  private const int MinDbReadsParallelism = 1;

  /// <summary>
  /// Максимально допустимое количество параллельных операций чтения из БД.
  /// </summary>
  private const int MaxDbReadsParallelism = 16;

  /// <summary>
  /// Минимально допустимый размер пакета обработки.
  /// </summary>
  private const int MinBatchSize = 1;

  /// <summary>
  /// Максимальное количество одновременно выполняемых задач миграции.
  /// </summary>
  /// <value>
  /// Число от <see cref="MinParallelism"/> до <see cref="MaxParallelism"/>.
  /// </value>
  /// <remarks>
  /// Значение определяет уровень конкурентного выполнения операций (I/O-bound).
  /// Рекомендуется подбирать экспериментально в зависимости от
  /// производительности СУБД, файлового хранилища и доступных ресурсов сервера.
  /// Обычно начальное значение устанавливается равным <c>Environment.ProcessorCount * 2</c>.
  /// </remarks>
  [Required]
  [Range(MinParallelism, MaxParallelism)]
  public int? MaxDegreeOfParallelism { get; set; }

  /// <summary>
  /// Максимальное количество параллельных операций чтения из базы данных.
  /// </summary>
  /// <value>
  /// Значение в диапазоне от <see cref="MinDbReadsParallelism"/>
  /// до <see cref="MaxDbReadsParallelism"/>.
  /// </value>
  /// <remarks>
  /// Ограничение помогает предотвратить избыточную нагрузку
  /// на SQL Server при выполнении массовых выборок.
  /// Значение рекомендуется подбирать экспериментально
  /// с учётом ресурсов сервера базы данных.
  /// Ориентир: ≈ (число ядер / 2).
  /// </remarks>
  [Required]
  [Range(MinDbReadsParallelism, MaxDbReadsParallelism)]
  public int MaxParallelDbReads { get; set; }

  /// <summary>
  /// Размер пакета обрабатываемых документов.
  /// </summary>
  /// <value>
  /// Положительное целое число, определяющее количество записей,
  /// извлекаемых и обрабатываемых за одну итерацию.
  /// </value>
  [Required]
  [Range(MinBatchSize, int.MaxValue)]
  public int? BatchSize { get; set; }

  /// <summary>
  /// Параметры отказоустойчивости.
  /// </summary>
  /// <value>
  /// Настройки стратегий Retry, Timeout и других механизмов Polly.
  /// </value>
  [Required]
  public ResilienceOptions Resilience { get; set; } = new();

  /// <summary>
  /// Параметры отказоустойчивости.
  /// </summary>
  public sealed class ResilienceOptions
  {
    /// <summary>
    /// Минимально допустимое количество повторных попыток.
    /// </summary>
    private const int MinRetryAttempts = 0;

    /// <summary>
    /// Максимально допустимое количество повторных попыток.
    /// </summary>
    private const int MaxRetryAttempts = 10;

    /// <summary>
    /// Максимальное количество повторных попыток при transient‑ошибках SQL.
    /// </summary>
    /// <value>
    /// Целое число в диапазоне от <c><see cref="MinRetryAttempts"/></c> до <c><see cref="MaxRetryAttempts"/></c>.
    /// Значение <c>0</c> отключает retry‑механику.
    /// </value>
    /// <remarks>
    /// Используется стратегией Retry в Polly.
    /// Рекомендуемое значение для production — 2–5.
    /// </remarks>
    [Required]
    [Range(MinRetryAttempts, MaxRetryAttempts)]
    public int SqlMaxRetryAttempts { get; set; }
  }
}
