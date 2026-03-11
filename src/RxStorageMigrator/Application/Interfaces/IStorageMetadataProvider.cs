using RxStorageMigrator.Application.Models;

namespace RxStorageMigrator.Application.Interfaces;

/// <summary>
/// Предоставляет метаданные о хранилищах файлов, используемых в процессе миграции.
/// </summary>
public interface IStorageMetadataProvider
{
  /// <summary>
  /// Коллекция хранилищ Directum 5,
  /// где ключ — идентификатор хранилища.
  /// </summary>
  /// <value>
  /// Словарь, где ключ — идентификатор хранилища,
  /// значение — описание файлового хранилища.
  /// </value>
  Dictionary<long, FileStorage> D5Storages { get; }

  /// <summary>
  /// Коллекция хранилищ Directum RX,
  /// где ключ — идентификатор хранилища.
  /// </summary>
  /// <value>
  /// Словарь, где ключ — идентификатор хранилища,
  /// значение — описание файлового хранилища.
  /// </value>
  Dictionary<long, FileStorage> RxStorages { get; }

  /// <summary>
  /// Выполнить инициализацию хранилищ.
  /// </summary>
  /// <param name="cancellationToken">Токен отмены, позволяющий прервать выполнение операции.</param>
  /// <returns>Задача, представляющая асинхронную операцию инициализации.</returns>
  Task InitializeAsync(CancellationToken cancellationToken);
}
