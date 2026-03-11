namespace RxStorageMigrator.Application.Models;

/// <summary>
/// Результат миграции записи.
/// </summary>
/// <param name="RowId">Идентификатор строки.</param>
/// <param name="XRecId">Идентификатор версии в Directum 5.</param>
/// <param name="Number">Номер версии.</param>
/// <param name="RxDocId">Идентификатор документа в целевой системе RX.</param>
/// <param name="BodyId">Идентификатор тела документа.</param>
/// <param name="MigrationStatus">Статус выполнения миграции.</param>
/// <param name="Comment">Комментарий.</param>
/// <param name="D5FilePath">Путь к исходному файлу в системе Directum 5 (если есть).</param>
/// <param name="RxFilePath">Путь к файлу в целевой системе RX.</param>
public sealed record MigrationResult(
  long RowId,
  int XRecId,
  int Number,
  long RxDocId,
  Guid BodyId,
  string MigrationStatus,
  string? Comment,
  string? D5FilePath,
  string RxFilePath);
