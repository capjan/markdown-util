using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using Serilog;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;

class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    readonly AbsolutePath CliProjectFolder = RootDirectory / "Source" / "MarkdownUtil";
    readonly AbsolutePath PackagePath = RootDirectory / "Source" / "MarkdownUtil" / "nupkg";

    [Parameter("NuGet Api key for publishing to nuget.org")] [Secret] readonly string NuGetApiKey;
    
    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            DotNetTasks.DotNetClean(_ => _
                .SetConfiguration(Configuration));
        });

    Target Info => _ => _
        .Executes(() =>
        {
            Log.Information("Configuration: {Configuration}", Configuration);
        });
    
    Target Restore => _ => _
        .DependsOn(Info)
        .Executes(() =>
        {
            DotNetTasks.DotNetRestore();
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetTasks.DotNetBuild(_ => _
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    // Test are always executed in Release configuration
    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTasks.DotNetTest(_ => _
                .When(Configuration == global::Configuration.Release, _ => _
                    .EnableNoRestore()
                    .EnableNoBuild())
                .SetConfiguration(global::Configuration.Release));
        });

    // We are forcing Release configuration for packaging, because we do not want any debug builds in the wild
    Target Pack => _ => _
        .DependsOn(Test)
        .Executes(() =>
        {
            PackagePath.GlobFiles("*.nupkg").ForEach(DeleteFile);
            DotNetTasks.DotNetPack(_ => _
                .SetProject(CliProjectFolder)
                .SetOutputDirectory(PackagePath)
                .When(Configuration == global::Configuration.Release, _ => _
                    .EnableNoRestore()
                    .EnableNoBuild())
                .SetConfiguration(Configuration.Release));
        });

    Target Deploy => _ => _
        .DependsOn(Pack)
        .Requires(() => NuGetApiKey)
        .Executes(() =>
        {
            DotNetTasks.DotNetNuGetPush(_ => _
                .SetTargetPath(PackagePath / "*.nupkg")
                .SetApiKey(NuGetApiKey)
                .SetSource("https://api.nuget.org/v3/index.json")
                .EnableSkipDuplicate()
            );
        });
}
