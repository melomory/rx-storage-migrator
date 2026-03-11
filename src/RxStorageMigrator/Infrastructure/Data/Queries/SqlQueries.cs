using System.Reflection;

namespace RxStorageMigrator.Infrastructure.Data.Queries;

/// <summary>
/// Предоставляет доступ к встроенным SQL‑скриптам, загружаемым из ресурсов сборки.
/// </summary>
/// <remarks>
/// SQL‑файлы должны быть добавлены в проект как Embedded Resource
/// и располагаться в пространстве имён с префиксом <c>Sql.</c>.
/// Имя ресурса формируется по шаблону:
/// <c>Sql.{ИмяСвойства}.sql</c>.
/// </remarks>
public static class SqlQueries
{
  /// <summary>
  /// Префикс пространства имён для SQL‑ресурсов.
  /// </summary>
  private const string Prefix = "Sql.";

  /// <summary>
  /// Сборка, содержащая встроенные SQL‑ресурсы.
  /// </summary>
  private static readonly Assembly Assembly = typeof(SqlQueries).Assembly;

  /// <summary>SQL‑запрос для подсчёта мигрированных документов.</summary>
  public static readonly string SelectCountMigratedDocuments = Load(nameof(SelectCountMigratedDocuments));

  /// <summary>SQL‑запрос для получения мигрированных документов пачками.</summary>
  public static readonly string SelectMigratedDocumentsPaged = Load(nameof(SelectMigratedDocumentsPaged));

  /// <summary>SQL‑запрос для получения версии файла.</summary>
  public static readonly string SelectFileVersion = Load(nameof(SelectFileVersion));

  /// <summary>SQL‑запрос для получения списка хранилищ Directum 5.</summary>
  public static readonly string SelectD5Storages = Load(nameof(SelectD5Storages));

  /// <summary>SQL‑запрос для получения списка хранилищ Directum RX.</summary>
  public static readonly string SelectRxStorages = Load(nameof(SelectRxStorages));

  /// <summary>SQL‑скрипт создания таблицы очереди миграции.</summary>
  public static readonly string CreateMigrationQueueTable = Load(nameof(CreateMigrationQueueTable));

  /// <summary>SQL‑скрипт наполнения очереди миграции.</summary>
  public static readonly string InsertMigrationQueue = Load(nameof(InsertMigrationQueue));

  /// <summary>SQL‑скрипт обновления очереди перед началом миграции.</summary>
  public static readonly string UpdateMigrationQueueBeforeStart = Load(nameof(UpdateMigrationQueueBeforeStart));

  /// <summary>SQL‑скрипт создания табличного типа результатов миграции.</summary>
  public static readonly string CreateMigrationFileResultTableType = Load(nameof(CreateMigrationFileResultTableType));

  /// <summary>SQL‑скрипт создания хранимой процедуры сохранения результатов.</summary>
  public static readonly string CreateSaveMigrationResultsStoredProcedure =
    Load(nameof(CreateSaveMigrationResultsStoredProcedure));

  /// <summary>SQL‑запрос для подсчёта документов в очереди.</summary>
  public static readonly string SelectCountQueuedMigratedDocuments = Load(nameof(SelectCountQueuedMigratedDocuments));

  /// <summary>
  /// SQL‑запрос для подсчёта документов в очереди, готовых к передаче.
  /// </summary>
  public static readonly string SelectCountQueuedMigratedDocumentsForTransfer =
    Load(nameof(SelectCountQueuedMigratedDocumentsForTransfer));

  /// <summary>
  /// SQL‑запрос для очистки таблицы очереди миграции.
  /// </summary>
  public static readonly string TruncateMigrationQueueTable =
    Load(nameof(TruncateMigrationQueueTable));

  /// <summary>
  /// SQL‑запрос для очистки таблицы перенесенных документов.
  /// </summary>
  public static readonly string TruncateTransferredTable =
    Load(nameof(TruncateTransferredTable));

  /// <summary>
  /// SQL‑запрос для получения количества перенесенных документов.
  /// </summary>
  public static readonly string SelectCountTransferredDocuments =
    Load(nameof(SelectCountTransferredDocuments));

  /// <summary>
  /// Загрузить SQL‑скрипт из встроенных ресурсов сборки.
  /// </summary>
  /// <param name="name">Имя SQL‑ресурса (без префикса и расширения).</param>
  /// <returns>Текст SQL‑скрипта.</returns>
  /// <exception cref="InvalidOperationException">Указанный ресурс не найден в сборке.</exception>
  /// <remarks>Ожидается, что ресурс имеет имя <c>Sql.{name}.sql</c>.</remarks>
  private static string Load(string name)
  {
    var resourceName = $"{Prefix}{name}.sql";

    using var stream = Assembly.GetManifestResourceStream(resourceName) ??
                       throw new InvalidOperationException($"SQL resource not found: {resourceName}");

    using var reader = new StreamReader(stream);

    return reader.ReadToEnd();
  }
}
