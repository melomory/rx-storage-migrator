using Nuke.Common;
using Nuke.Common.IO;
using System.IO;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

class Build : NukeBuild
{
  /// Support plugins are available for:
  ///   - JetBrains ReSharper        https://nuke.build/resharper
  ///   - JetBrains Rider            https://nuke.build/rider
  ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
  ///   - Microsoft VSCode           https://nuke.build/vscode
  public static int Main() => Execute<Build>(x => x.Run);

  [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
  readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

  [Parameter("Publish as self-contained")]
  readonly bool SelfContained;

  [Parameter("Target runtime (e.g. win-x64, linux-x64, osx-x64)")]
  readonly string Runtime = "win-x64";

  [Parameter("Minimum line coverage threshold")]
  readonly int CoverageThreshold = 18;

  [Parameter]
  readonly string PublishDir;

  [GitVersion]
  readonly GitVersion GitVersion;

  AbsolutePath SourceDirectory => RootDirectory / "src";

  AbsolutePath TestsDirectory => RootDirectory / "tests";

  AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

  AbsolutePath CoverageDirectory => ArtifactsDirectory / "coverage";

  AbsolutePath TestResultsDirectory => ArtifactsDirectory / "test-results";

  AbsolutePath PublishDirectory => string.IsNullOrWhiteSpace(PublishDir)
    ? ArtifactsDirectory / "publish"
    : RootDirectory / PublishDir;

  AbsolutePath Project => SourceDirectory / "RxStorageMigrator" / "RxStorageMigrator.csproj";

  AbsolutePath TestProject => TestsDirectory / "RxStorageMigrator.UnitTests" / "RxStorageMigrator.UnitTests.csproj";

  Target Clean => t => t
    .Before(Restore)
    .Executes(
      () =>
      {
        DotNetClean();

        if (Directory.Exists(ArtifactsDirectory))
          Directory.Delete(ArtifactsDirectory, true);
      });

  Target Restore => t => t
    .Executes(() => { DotNetRestore(); });

  Target Compile => t => t
    .DependsOn(Restore)
    .Executes(
      () =>
      {
        DotNetBuild(
          s => s
            .SetConfiguration(Configuration)
            .EnableNoRestore());
      });

  Target Test => t => t
    .DependsOn(Compile)
    .Executes(
      () =>
      {
        CoverageDirectory.CreateDirectory();

        DotNetTest(
          s => s
            .SetProjectFile(TestProject)
            .SetConfiguration(Configuration)
            .EnableNoBuild()
            .SetLoggers("trx")
            .SetResultsDirectory(TestResultsDirectory)
            .SetProperty("CollectCoverage", "true")
            .SetProperty("CoverletOutputFormat", "cobertura")
            .SetProperty("CoverletOutput", (CoverageDirectory / "coverage").ToString())
            .SetProperty("Threshold", CoverageThreshold.ToString())
            .SetProperty("ThresholdType", "line")
            .SetProperty("ThresholdStat", "total"));
      });

  Target Run => t => t
    .Executes(
      () =>
      {
        DotNetRun(
          s => s
            .SetProjectFile(Project));
      });

  Target Publish => t => t
    .DependsOn(Compile)
    .Executes(
      () =>
      {
        Directory.CreateDirectory(PublishDirectory);

        DotNetPublish(
          s => s
            .SetProject(Project)
            .SetConfiguration(Configuration)
            .SetOutput(PublishDirectory)
            .SetRuntime(SelfContained ? Runtime : null)
            .SetSelfContained(SelfContained)
            .SetVersion(GitVersion.SemVer)
            .SetAssemblyVersion(GitVersion.AssemblySemVer)
            .SetFileVersion(GitVersion.AssemblySemFileVer)
            .SetInformationalVersion(GitVersion.InformationalVersion));
      });
}
