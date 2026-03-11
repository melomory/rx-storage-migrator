namespace RxStorageMigrator.Application.Interfaces;

/// <summary>
/// Контракт для сервиса, выполняющего процесс миграции.
/// </summary>
public interface IMigrationService
{
  /// <summary>
  /// Запустить процесс миграции.
  /// </summary>
  /// <param name="progress">
  /// Объект для отслеживания прогресса выполнения.
  /// Кортеж содержит количество обработанных элементов и их общее количество.
  /// </param>
  /// <param name="ct">Токен отмены, позволяющий прервать выполнение операции.</param>
  /// <returns>Задача, представляющая асинхронную операцию миграции.</returns>
  Task RunAsync(IProgress<(int Processed, int Total)> progress, CancellationToken ct);

  /// <summary>
  /// Асинхронно очищает данные, связанные с процессом миграции.
  /// </summary>
  /// <param name="ct">Токен отмены операции.</param>
  /// <returns>Задача, представляющая асинхронную операцию очистки.</returns>
  /// <remarks>
  /// Реализация должна гарантировать корректное
  /// удаление или сброс служебных таблиц/очередей,
  /// используемых при миграции.
  /// </remarks>
  Task ClearAsync(CancellationToken ct);
}
