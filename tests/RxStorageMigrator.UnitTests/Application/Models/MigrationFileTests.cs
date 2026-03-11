using FluentAssertions;
using RxStorageMigrator.UnitTests.TestHelpers;
using RxStorageMigrator.Application.Models;
using Xunit;

namespace RxStorageMigrator.UnitTests.Application.Models;

public class MigrationFileTests
{
  [Fact]
  public void SourceFolderPath_ShouldBeCorrect()
  {
    var file = TestDataBuilder.CreateFile(d5DocId: 12345, versionNumber: 7);

    var expected = Path.Combine(
      "/source",
      "12",
      "12345",
      "7");

    file.SourceFolderPath.Should().Be(expected);
  }

  [Fact]
  public void TargetFilePath_ShouldContainBodyIdAndBlobExtension()
  {
    var bodyId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    var file = TestDataBuilder.CreateFile(bodyId: bodyId);

    file.TargetFilePath.Should().EndWith($"{bodyId}.blob");
  }

  [Fact]
  public void GetTargetFolder_ShouldStartWithTargetStoragePath()
  {
    var file = TestDataBuilder.CreateFile();

    var folder = file.GetTargetFolder();

    folder.Should().StartWith("/target");
  }

  [Fact]
  public void GetTargetFolder_SameGuid_ShouldReturnSamePath()
  {
    var guid = Guid.NewGuid();

    var file1 = TestDataBuilder.CreateFile(bodyId: guid);
    var file2 = TestDataBuilder.CreateFile(bodyId: guid);

    file1.GetTargetFolder()
      .Should()
      .Be(file2.GetTargetFolder());
  }

  [Fact]
  public void GetTargetFolder_DifferentGuids_ShouldUsuallyDiffer()
  {
    var file1 = TestDataBuilder.CreateFile(bodyId: Guid.NewGuid());
    var file2 = TestDataBuilder.CreateFile(bodyId: Guid.NewGuid());

    file1.GetTargetFolder()
      .Should()
      .NotBe(file2.GetTargetFolder());
  }

  [Fact]
  public void GetTargetFolder_ShouldContainTwoSubfolders()
  {
    var file = TestDataBuilder.CreateFile();

    var relativePart = file.GetTargetFolder()
      .Replace("/target" + Path.DirectorySeparatorChar, "");

    var segments = relativePart.Split(Path.DirectorySeparatorChar);

    segments.Length.Should().Be(2);
  }

  [Fact]
  public void GetTargetFolder_SubfoldersShouldBeNumeric()
  {
    var file = TestDataBuilder.CreateFile();

    var relative = file.GetTargetFolder()
      .Replace("/target" + Path.DirectorySeparatorChar, "");

    var parts = relative.Split(Path.DirectorySeparatorChar);

    parts.All(p => long.TryParse(p, out _)).Should().BeTrue();
  }
}
