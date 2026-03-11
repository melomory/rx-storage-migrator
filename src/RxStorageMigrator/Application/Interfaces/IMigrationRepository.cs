using RxStorageMigrator.Application.Models;

namespace RxStorageMigrator.Application.Interfaces;

/// <summary>
/// Контракт репозитория для получения данных,
/// необходимых для миграции файлов, и сохранения результатов миграции.
/// </summary>
public interface IMigrationRepository
{
  /// <summary>
  /// Выполнить инициализацию инфраструктуры миграции
  /// (например, создаёт временные таблицы, индексы или служебные структуры).
  /// </summary>
  /// <param name="ct">Токен отмены операции.</param>
  /// <returns>Асинхронная задача выполнения инициализации.</returns>
  Task InitializeMigrationInfrastructureAsync(CancellationToken ct);

  /// <summary>
  /// Получить общее количество уже мигрированных документов.
  /// </summary>
  /// <param name="ct">Токен отмены операции.</param>
  /// <returns>Общее количество документов, успешно прошедших миграцию.</returns>
  Task<int> GetTotalMigratedDocumentsCountAsync(CancellationToken ct);

  /// <summary>
  /// Получить общее количество документов, поставленных в очередь на перенос.
  /// </summary>
  /// <param name="ct">Токен отмены операции.</param>
  /// <returns>Общее количество документов, находящихся в очереди.</returns>
  Task<int> GetTotalQueuedForTransferCountAsync(CancellationToken ct);

  /// <summary>
  /// Получить количество документов, доступных для обработки в текущий момент.
  /// </summary>
  /// <param name="ct">Токен отмены операции.</param>
  /// <returns>Количество документов, готовых к переносу.</returns>
  Task<int> GetQueuedForTransferCountAsync(CancellationToken ct);

  /// <summary>
  /// Получить количество перенесенных документов.
  /// </summary>
  /// <param name="ct">Токен отмены операции.</param>
  /// <returns>Количество документов.</returns>
  Task<TransferredCountResult> GetTransferredDocumentsCountAsync(CancellationToken ct);

  /// <summary>
  /// Возвращает асинхронную последовательность файлов, подготовленных к миграции.
  /// </summary>
  /// <param name="batchSize">Максимальное количество записей, извлекаемых за одну итерацию.</param>
  /// <param name="ct">Токен отмены операции.</param>
  /// <returns>Асинхронная последовательность объектов <see cref="MigrationFile"/>.</returns>
  /// <remarks>
  /// Метод должен обеспечивать потокобезопасное получение данных
  /// при параллельной обработке.
  /// </remarks>
  IAsyncEnumerable<MigrationFile> GetFilesAsync(int batchSize, CancellationToken ct);

  /// <summary>
  /// Сохранить результаты обработки пакета файлов.
  /// </summary>
  /// <param name="results">Результаты миграции для сохранения.</param>
  /// <param name="ct">Токен отмены операции.</param>
  /// <returns>Асинхронная задача сохранения результатов.</returns>
  Task SaveMigrationResultsAsync(IReadOnlyCollection<MigrationResult> results, CancellationToken ct);

  /// <summary>
  /// Асинхронно очищает таблицы, используемые в процессе миграции.
  /// </summary>
  /// <param name="ct">Токен отмены операции.</param>
  /// <returns>Задача, представляющая асинхронную операцию очистки таблиц.</returns>
  /// <remarks>
  /// Реализация должна корректно удалить или сбросить данные
  /// служебных таблиц (очередь миграции, результаты и др.),
  /// обеспечивая целостность данных.
  /// Метод должен быть идемпотентным.
  /// </remarks>
  Task ClearTablesAsync(CancellationToken ct);
}
