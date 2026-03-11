using System.Globalization;

namespace RxStorageMigrator.Application.Models;

/// <summary>
/// Представляет файл, подлежащий миграции, включая информацию об исходном и целевом хранилищах.
/// </summary>
/// <param name="RowId">Идентификатор строки в таблице очереди миграции.</param>
/// <param name="D5DocId">Идентификатор документа в системе Directum 5.</param>
/// <param name="VersionNumber">Номер версии документа в Directum 5.</param>
/// <param name="BodyId">Идентификатор содержимого (тела) документа в целевой системе.</param>
/// <param name="VersionId">Идентификатор версии документа.</param>
/// <param name="Extension">Расширение файла (без точки).</param>
/// <param name="SourceFileStorage">Исходное файловое хранилище.</param>
/// <param name="RxDocId">Идентификатор документа в системе Directum RX.</param>
/// <param name="TargetFileStorage">Целевое файловое хранилище.</param>
public record MigrationFile(
  long RowId,
  int D5DocId,
  int VersionNumber,
  Guid BodyId,
  int VersionId,
  string Extension,
  FileStorage SourceFileStorage,
  long RxDocId,
  FileStorage TargetFileStorage)
{
  /// <summary>
  /// Размер группы документов Directum 5, используемый для формирования структуры папок.
  /// </summary>
  private const int D5DocIdGroupSize = 1_000;

  /// <summary>
  /// Диапазон значений для нормализации хеша файла.
  /// </summary>
  private const long FileDistributionRange = 100_000_000L;

  /// <summary>
  /// Размер сегмента распределения файлов по подпапкам.
  /// </summary>
  private const long FileDistributionSegmentSize = 10_000L;

  /// <summary>
  /// Полный путь к целевому файлу в хранилище RX.
  /// </summary>
  /// <value>
  /// Абсолютный путь к файлу с именем <c>{BodyId}.blob</c>, в целевом хранилище.
  /// </value>
  public string TargetFilePath => Path.Combine(GetTargetFolder(), $"{BodyId}.blob");

  /// <summary>
  /// Полный путь к директории исходного файла в Directum 5.
  /// </summary>
  /// <value>
  /// Путь формируется по шаблону:
  /// <c>{StoragePath}\{D5DocId / <see cref="D5DocIdGroupSize"/>}\{D5DocId}\{VersionNumber}</c>.
  /// </value>
  public string SourceFolderPath =>
    Path.Combine(
      SourceFileStorage.Path,
      (D5DocId / D5DocIdGroupSize).ToString(CultureInfo.InvariantCulture),
      D5DocId.ToString(CultureInfo.InvariantCulture),
      VersionNumber.ToString(CultureInfo.InvariantCulture));

  /// <summary>
  /// Вычислить относительный путь к файлу на основе его идентификатора.
  /// </summary>
  /// <param name="bodyId">Идентификатор содержимого документа.</param>
  /// <returns>Относительный путь из двух уровней вложенности.</returns>
  /// <remarks>
  /// Алгоритм распределения:
  /// <list type="number">
  /// <item><description>GUID преобразуется в 64‑битное число.</description></item>
  /// <item><description>Берётся абсолютное значение для исключения отрицательных чисел.</description></item>
  /// <item><description>Значение нормализуется в пределах <see cref="FileDistributionRange"/>.</description></item>
  /// <item><description>
  /// Вычисляются две директории:
  /// первая — деление на <see cref="FileDistributionSegmentSize"/>,
  /// вторая — остаток от деления.
  /// </description></item>
  /// </list>
  /// Это предотвращает хранение большого количества файлов в одной папке
  /// и улучшает производительность файловой системы.
  /// </remarks>
  private static string GetRelativeFilePath(Guid bodyId)
  {
    var hash = Math.Abs(BitConverter.ToInt64(bodyId.ToByteArray(), 0));
    var normalized = hash % FileDistributionRange;

    var folder = normalized % FileDistributionSegmentSize;
    var subFolder = normalized / FileDistributionSegmentSize;

    return Path.Combine(
      folder.ToString(CultureInfo.InvariantCulture),
      subFolder.ToString(CultureInfo.InvariantCulture));
  }

  /// <summary>
  /// Получить путь до данных в хранилище.
  /// </summary>
  /// <returns>Путь до данных в хранилище.</returns>
  public string GetTargetFolder() => Path.Combine(TargetFileStorage.Path, GetRelativeFilePath(BodyId));
}
