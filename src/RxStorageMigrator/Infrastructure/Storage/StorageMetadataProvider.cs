using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RxStorageMigrator.Application.Interfaces;
using RxStorageMigrator.Application.Models;
using RxStorageMigrator.Configuration;
using RxStorageMigrator.Infrastructure.Data.Queries;

namespace RxStorageMigrator.Infrastructure.Storage;

/// <summary>
/// Провайдер метаданных хранилища из базы данных <see cref="IStorageMetadataProvider"/>.
/// </summary>
/// <remarks>
/// При инициализации выполняет подключение к БД Directum 5 и загружает
/// информацию об исходных (D5) и целевых (Rx) хранилищах.
/// </remarks>
public sealed class StorageMetadataProvider : IStorageMetadataProvider
{
  /// <summary>
  /// Логгер.
  /// </summary>
  private readonly ILogger<StorageMetadataProvider> _logger;

  /// <summary>
  /// Строка подключения.
  /// </summary>
  private readonly string _connectionString;

  /// <summary>
  /// Создаёт экземпляр <see cref="StorageMetadataProvider"/>.
  /// </summary>
  /// <param name="logger">Логгер для записи диагностической информации.</param>
  /// <param name="databaseOptions">
  /// Настройки подключения к базе данных. Используется строка подключения к БД D5.
  /// </param>
  public StorageMetadataProvider(ILogger<StorageMetadataProvider> logger, IOptions<DatabaseOptions> databaseOptions)
  {
    _logger = logger;
    _connectionString = databaseOptions.Value.SqlConnectionStringD5!;
  }

  /// <inheritdoc />
  public Dictionary<long, FileStorage> D5Storages { get; private set; } = [];

  /// <inheritdoc />
  public Dictionary<long, FileStorage> RxStorages { get; private set; } = [];

  /// <summary>
  /// Выполнить инициализацию провайдера, загружая данные о хранилищах из базы данных.
  /// </summary>
  /// <param name="cancellationToken">Токен отмены, позволяющий прервать операцию загрузки.</param>
  /// <returns>Асинхронная задача инициализации.</returns>
  public async Task InitializeAsync(CancellationToken cancellationToken)
  {
    _logger.LogDebug("Initializing {Service}", nameof(StorageMetadataProvider));

    await using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync(cancellationToken);

    _logger.LogDebug("Loading D5 storages");
    var d5Storages = await connection.QueryAsync<FileStorage>(SqlQueries.SelectD5Storages);
    D5Storages = d5Storages.ToDictionary(x => x.Id);

    _logger.LogDebug("Loading Rx storages");
    var rxStorages = await connection.QueryAsync<FileStorage>(SqlQueries.SelectRxStorages);
    RxStorages = rxStorages.ToDictionary(x => x.Id);

    _logger.LogInformation("Loaded storages: D5={D5Count}, Rx={RxCount}", D5Storages.Count, RxStorages.Count);
  }
}
