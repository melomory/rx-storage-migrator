using RxStorageMigrator.Application.Models;
using RxStorageMigrator.Infrastructure.Data.Mappings;

namespace RxStorageMigrator.UnitTests.TestHelpers;

public static class TestDataBuilder
{
  public static FileStorage CreateStorage(
    long id = 1,
    string name = "Storage",
    string storageTypeCode = "FS",
    string path = "/tmp") =>
    new(
      id,
      name,
      storageTypeCode,
      path);

  public static MigrationFile CreateFile(
    long rowId = 1,
    Guid? bodyId = null,
    int d5DocId = 1000,
    int versionNumber = 1) =>
    new MigrationFile(
      RowId: rowId,
      D5DocId: d5DocId,
      VersionNumber: versionNumber,
      BodyId: bodyId ?? Guid.NewGuid(),
      VersionId: 1,
      Extension: "pdf",
      SourceFileStorage: CreateStorage(id: 10, path: "/source"),
      RxDocId: 5000,
      TargetFileStorage: CreateStorage(id: 20, path: "/target"));

  public static IReadOnlyCollection<MigrationFile> CreateBatch(int count) =>
    Enumerable.Range(1, count)
      .Select(i => CreateFile(i))
      .ToList();

  public static MigrationResult CreateResult(
    MigrationFile file,
    MigrationStatus status) =>
    new(
      file.RowId,
      file.D5DocId,
      file.VersionNumber,
      file.RxDocId,
      file.BodyId,
      status.ToDb(),
      null,
      null,
      file.TargetFileStorage.Path);
}
