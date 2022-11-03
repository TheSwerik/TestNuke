using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Tools.GitVersion;
using Serilog;

[GitHubActions(
    "continuous",
    GitHubActionsImage.UbuntuLatest,
    On = new[] { GitHubActionsTrigger.Push },
    InvokedTargets = new[] { nameof(Compile) }
)]
class Build : NukeBuild
{
    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [GitVersion] readonly GitVersion GitVersion;
    GitHubActions GitHubActions => GitHubActions.Instance;

    Target Clean => _ => _
                         .Before(Restore)
                         .Executes(() =>
                                   {
                                   });

    Target Restore => _ => _
                          .Executes(() =>
                                    {
                                    });

    Target Compile => _ => _
                           .DependsOn(Restore)
                           .Triggers(Print)
                           .Executes(() =>
                                     {
                                     });

    Target Print => _ => _
                         .OnlyWhenStatic(() => IsLocalBuild)
                         .Executes(() =>
                                   {
                                       Log.Information("IsLocalBuild = {Value}", IsLocalBuild);
                                       if (IsLocalBuild) return;
                                       Log.Information("GitVersion = {Value}", GitVersion.MajorMinorPatch);
                                       Log.Information("Branch = {Branch}", GitHubActions.Ref);
                                       Log.Information("Commit = {Commit}", GitHubActions.Sha);
                                       Log.Information("RunNumber = {Commit}", GitHubActions.RunNumber);
                                       Log.Information("RunId = {Commit}", GitHubActions.RunId);
                                   });

    /// Support plugins are available for:
    /// - JetBrains ReSharper        https://nuke.build/resharper
    /// - JetBrains Rider            https://nuke.build/rider
    /// - Microsoft VisualStudio     https://nuke.build/visualstudio
    /// - Microsoft VSCode           https://nuke.build/vscode
    public static int Main() => Execute<Build>(x => x.Print);
}