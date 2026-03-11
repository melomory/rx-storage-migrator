using System.Collections.Concurrent;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using RxStorageMigrator.Application.Common;
using RxStorageMigrator.Application.Interfaces;
using RxStorageMigrator.Application.Models;
using RxStorageMigrator.Application.Services;
using RxStorageMigrator.Configuration;
using RxStorageMigrator.Infrastructure.Data.Mappings;
using RxStorageMigrator.UnitTests.TestHelpers;

namespace RxStorageMigrator.UnitTests.Application.Services;

public class MigrationServiceTests
{
  private readonly IMigrationRepository _repository = Substitute.For<IMigrationRepository>();

  private readonly IFileMigrator _fileMigrator = Substitute.For<IFileMigrator>();

  private readonly IStorageMetadataProvider _metadataProvider = Substitute.For<IStorageMetadataProvider>();

  private readonly IConsoleNotifier _consoleNotifier = Substitute.For<IConsoleNotifier>();

  private readonly ILogger<MigrationService> _logger = Substitute.For<ILogger<MigrationService>>();

  private readonly MigrationService _sut;

  public MigrationServiceTests()
  {
    var appOptions = Microsoft.Extensions.Options.Options.Create(
      new RxStorageMigrator.Configuration.AppOptions
      {
        BatchSize = 2,
        MaxDegreeOfParallelism = 2,
      });

    var dbOptions = Options.Create(new DatabaseOptions { SqlConnectionStringD5 = "Server=test;Password=Secret;" });

    _sut = new MigrationService(
      _repository,
      _fileMigrator,
      appOptions,
      dbOptions,
      _logger,
      _consoleNotifier,
      _metadataProvider);
  }

  [Fact]
  public async Task RunAsync_WhenQueuedForTransferIsZero_ShouldExitEarly()
  {
    _repository.GetTotalMigratedDocumentsCountAsync(Arg.Any<CancellationToken>())
      .Returns(10);

    _repository.GetTotalQueuedForTransferCountAsync(Arg.Any<CancellationToken>())
      .Returns(5);

    _repository.GetQueuedForTransferCountAsync(Arg.Any<CancellationToken>())
      .Returns(0);

    var reported = new List<(int, int)>();
    var progress = new Progress<(int, int)>(x => reported.Add(x));

    await _sut.RunAsync(progress, CancellationToken.None);

    reported.Should().ContainSingle();
    reported[0].Should().Be((0, 0));

    await _repository.DidNotReceive()
      .SaveMigrationResultsAsync(Arg.Any<IReadOnlyCollection<MigrationResult>>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task RunAsync_ShouldProcessFiles_AndSaveResults()
  {
    var files = TestDataBuilder.CreateBatch(2);

    _repository.GetTotalMigratedDocumentsCountAsync(Arg.Any<CancellationToken>())
      .Returns(0);

    _repository.GetTotalQueuedForTransferCountAsync(Arg.Any<CancellationToken>())
      .Returns(2);

    _repository.GetQueuedForTransferCountAsync(Arg.Any<CancellationToken>())
      .Returns(2);

    _repository.GetFilesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
      .Returns(
        ToAsyncEnumerable(files),
        ToAsyncEnumerable(Array.Empty<MigrationFile>())
      );

    _fileMigrator.MigrateAsync(Arg.Any<MigrationFile>(), Arg.Any<CancellationToken>())
      .Returns(
        ci =>
        {
          var file = ci.Arg<MigrationFile>();

          return Task.FromResult(
            Result<MigrationResult>.Success(
              file.ToMigrationResult(
                MigrationStatus.Performed,
                null)));
        });

    var progressEvents = new ConcurrentBag<(int, int)>();
    var progress = new Progress<(int, int)>(x => progressEvents.Add(x));

    _repository.GetTransferredDocumentsCountAsync(Arg.Any<CancellationToken>())
      .Returns(
        new TransferredCountResult(
          2,
          2));

    await _sut.RunAsync(progress, CancellationToken.None);

    await _repository.Received(1)
      .SaveMigrationResultsAsync(
        Arg.Is<IReadOnlyCollection<MigrationResult>>(
          r =>
            r.Count == 2 &&
            r.All(x => x.MigrationStatus == MigrationStatus.Performed.ToDb())),
        Arg.Any<CancellationToken>());

    progressEvents.Should()
      .Contain(x => x.Item1 == 2 && x.Item2 == 2);
  }

  [Fact]
  public async Task RunAsync_WhenFileMigratorThrows_ShouldReturnErrorResult()
  {
    var file = TestDataBuilder.CreateFile();

    _repository.GetTotalMigratedDocumentsCountAsync(CancellationToken.None).Returns(0);
    _repository.GetTotalQueuedForTransferCountAsync(CancellationToken.None).Returns(1);
    _repository.GetQueuedForTransferCountAsync(CancellationToken.None).Returns(1);

    _repository.GetTransferredDocumentsCountAsync(Arg.Any<CancellationToken>())
      .Returns(new TransferredCountResult(0, 1));

    _repository.InitializeMigrationInfrastructureAsync(Arg.Any<CancellationToken>())
      .Returns(Task.CompletedTask);

    _metadataProvider.InitializeAsync(Arg.Any<CancellationToken>())
      .Returns(Task.CompletedTask);

    _repository.GetFilesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
      .Returns(
        ToAsyncEnumerable([file]),
        ToAsyncEnumerable(Array.Empty<MigrationFile>())
      );

    _fileMigrator.MigrateAsync(file, Arg.Any<CancellationToken>())
      .Returns<Task<Result<MigrationResult>>>(_ => throw new Exception("IO error"));

    var progress = new Progress<(int, int)>();

    await _sut.RunAsync(progress, CancellationToken.None);

    await _repository.Received()
      .SaveMigrationResultsAsync(
        Arg.Is<IReadOnlyCollection<MigrationResult>>(
          r =>
            r.Single().MigrationStatus == MigrationStatus.Error.ToDb()),
        Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task RunAsync_ShouldProcessMultipleBatches()
  {
    var firstBatch = TestDataBuilder.CreateBatch(1);
    var secondBatch = TestDataBuilder.CreateBatch(1);

    _repository.GetTotalMigratedDocumentsCountAsync(Arg.Any<CancellationToken>())
      .Returns(0);

    _repository.GetTotalQueuedForTransferCountAsync(Arg.Any<CancellationToken>())
      .Returns(2);

    _repository.GetQueuedForTransferCountAsync(Arg.Any<CancellationToken>())
      .Returns(2);

    _repository.InitializeMigrationInfrastructureAsync(Arg.Any<CancellationToken>())
      .Returns(Task.CompletedTask);

    _repository.GetTransferredDocumentsCountAsync(Arg.Any<CancellationToken>())
      .Returns(new TransferredCountResult(2, 2));

    _metadataProvider.InitializeAsync(Arg.Any<CancellationToken>())
      .Returns(Task.CompletedTask);

    _repository.GetFilesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
      .Returns(
        ToAsyncEnumerable(firstBatch),
        ToAsyncEnumerable(secondBatch),
        ToAsyncEnumerable(Array.Empty<MigrationFile>())
      );

    _fileMigrator.MigrateAsync(Arg.Any<MigrationFile>(), Arg.Any<CancellationToken>())
      .Returns(
        ci =>
        {
          var file = ci.Arg<MigrationFile>();

          return Task.FromResult(
            Result<MigrationResult>.Success(
              TestDataBuilder.CreateResult(file, MigrationStatus.Performed)));
        });

    var progress = new Progress<(int, int)>();

    await _sut.RunAsync(progress, CancellationToken.None);

    await _repository.Received(2)
      .SaveMigrationResultsAsync(
        Arg.Any<IReadOnlyCollection<MigrationResult>>(),
        Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task ClearAsync_ShouldCallRepository()
  {
    await _sut.ClearAsync(CancellationToken.None);

    await _repository.Received(1)
      .ClearTablesAsync(Arg.Any<CancellationToken>());
  }

  private static async IAsyncEnumerable<MigrationFile> ToAsyncEnumerable(IEnumerable<MigrationFile> files)
  {
    foreach (var file in files)
      yield return file;

    await Task.CompletedTask;
  }
}
