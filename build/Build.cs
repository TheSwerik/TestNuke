using System.IO;
using System.IO.Compression;
using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.IO.CompressionTasks;
using static Nuke.Common.IO.FileSystemTasks;

[GitHubActions(
    "continuous",
    GitHubActionsImage.UbuntuLatest,
    On = new[] { GitHubActionsTrigger.Push },
    InvokedTargets = new[] { nameof(Pack) }
)]
class Build : NukeBuild
{
    readonly AbsolutePath ArtifactPath = RootDirectory / "publish.zip";

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [GitVersion(Framework = "net6.0")] readonly GitVersion GitVersion;

    readonly AbsolutePath PublishDirectory = RootDirectory / "publish";

    [Solution] readonly Solution Solution;
    GitHubActions GitHubActions => GitHubActions.Instance;

    Target Clean => _ => _
                         .Before(Restore)
                         .Executes(() =>
                                   {
                                       DeleteFile(ArtifactPath);
                                       DeleteDirectory(PublishDirectory);
                                   });

    Target Restore => _ => _
                          .Executes(() =>
                                    {
                                        DotNetRestore(_ => _
                                                          .SetProjectFile(Solution));
                                    });

    Target Compile => _ => _
                           .DependsOn(Restore)
                           .Triggers(Print)
                           .Executes(() =>
                                     {
                                         DotNetBuild(_ => _
                                                          .EnableNoLogo()
                                                          .SetProjectFile(Solution)
                                                          .SetConfiguration(Configuration)
                                                          .EnableNoRestore());
                                     });

    Target Print => _ => _
                        .Executes(() =>
                                  {
                                      Log.Information("publish = {Value}", RootDirectory / "publish");
                                      Log.Information("BuildAssemblyDirectory = {Value}", BuildAssemblyDirectory);
                                      Log.Information("TemporaryDirectory = {Value}", TemporaryDirectory);
                                      Log.Information("RootDirectory = {Value}", RootDirectory);
                                      Log.Information("BuildProjectDirectory = {Value}", BuildProjectDirectory);
                                      Log.Information("IsLocalBuild = {Value}", IsLocalBuild);
                                      if (GitVersion is not null)
                                          Log.Information("GitVersion = {Value}", GitVersion.MajorMinorPatch);
                                      if (GitHubActions is null) return;
                                      Log.Information("Branch = {Branch}", GitHubActions.Ref);
                                      Log.Information("Commit = {Commit}", GitHubActions.Sha);
                                      Log.Information("RunNumber = {Commit}", GitHubActions.RunNumber);
                                      Log.Information("RunId = {Commit}", GitHubActions.RunId);
                                  });

    Target Pack => _ => _
                        .DependsOn(Compile)
                        .Produces(ArtifactPath)
                        .Executes(() =>
                                  {
                                      DotNetPublish(_ => _
                                                         .SetConfiguration(Configuration)
                                                         .SetProject(Solution)
                                                         .SetOutput(PublishDirectory));
                                      CompressZip(
                                          PublishDirectory,
                                          ArtifactPath,
                                          // filter: x => !x.Extension.EqualsAnyOrdinalIgnoreCase(ExcludedExtensions),
                                          compressionLevel: CompressionLevel.SmallestSize,
                                          fileMode: FileMode.CreateNew);
                                  })
                        .Executes(() =>
                                  {
                                  });

    /// Support plugins are available for:
    /// - JetBrains ReSharper        https://nuke.build/resharper
    /// - JetBrains Rider            https://nuke.build/rider
    /// - Microsoft VisualStudio     https://nuke.build/visualstudio
    /// - Microsoft VSCode           https://nuke.build/vscode
    public static int Main() => Execute<Build>(x => x.Pack);
}