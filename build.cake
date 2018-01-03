#tool nuget:?package=xunit.runner.console&version=2.3.1
#tool nuget:?package=xunit.runner.visualstudio&version=2.3.1
#tool nuget:?package=OpenCover&version=4.6.519
#tool nuget:?package=JetBrains.dotCover.CommandLineTools&version=2016.2.20160913.100041
#tool nuget:?package=ReportGenerator&version=3.1.0
#tool nuget:?package=GitVersion.CommandLine&version=3.6.5
#tool nuget:?package=OctopusTools&version=4.21.0

#load build/paths.cake
#load build/urls.cake

var target = Argument("Target", "Build");
var configuration = Argument("Configuration", "Release");
var codeCoverageReportPath = Argument<FilePath>("CodeCoverageReportPath", "coverage.zip");
var packageOutputPath = Argument<DirectoryPath>("PackageOutputPath", "packages");

var packageVersion = "0.1.0";

Task("Restore")
    .Does(() =>
{
    NuGetRestore(Paths.SolutionFile);
});

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
{
    DotNetBuild(
        Paths.SolutionFile,
        settings => settings.SetConfiguration(configuration)
                            .WithTarget("Build"));
});

Task("Test-OpenCover")
    .IsDependentOn("Build")
    .WithCriteria(() => BuildSystem.IsLocalBuild || BuildSystem.IsRunningOnAppVeyor)
    .Does(() =>
{
    OpenCover(
        tool => tool.XUnit2(
            $"**/bin/{configuration}/*Tests.dll",
            new XUnit2Settings
            {
                ShadowCopy = false
            }),
        Paths.CodeCoverageResultFile,
        new OpenCoverSettings()
            .WithFilter("+[Linker.*]*")
            .WithFilter("-[Linker.*Tests*]*")
    );
});

Task("Report-Coverage")
    .IsDependentOn("Test-OpenCover")
    .WithCriteria(() => BuildSystem.IsLocalBuild)
    .Does(() =>
{
    ReportGenerator(
        Paths.CodeCoverageResultFile,
        Paths.CodeCoverageReportDirectory,
        new ReportGeneratorSettings
        {
            ReportTypes = new[] { ReportGeneratorReportType.Html }
        }
    );

    Zip(
        Paths.CodeCoverageReportDirectory,
        MakeAbsolute(codeCoverageReportPath)
    );
});

Task("Version")
    .Does(() =>
{
    var version = GitVersion();
    Information($"Calculated semantic version {version.SemVer}");

    packageVersion = version.NuGetVersion;
    Information($"Corresponding package version {packageVersion}");

    if (!BuildSystem.IsLocalBuild)
    {
        GitVersion(new GitVersionSettings
        {
            OutputType = GitVersionOutput.BuildServer,
            UpdateAssemblyInfo = true
        });
    }
});

Task("Remove-Packages")
    .Does(() =>
{
    CleanDirectory(packageOutputPath);
});

Task("Package-NuGet")
    .IsDependentOn("Test")
    .IsDependentOn("Version")
    .IsDependentOn("Remove-Packages")
    .Does(() =>
{
    EnsureDirectoryExists(packageOutputPath);

    NuGetPack(
        Paths.WebNuspecFile,
        new NuGetPackSettings
        {
            Id = "Linker.Web", 
            Version = packageVersion,
            OutputDirectory = packageOutputPath,
            NoPackageAnalysis = true
        });

    NuGetPack(
        Paths.DatabaseNuspecFile,
        new NuGetPackSettings
        {
            Id = "Linker.Database", 
            Version = packageVersion,
            OutputDirectory = packageOutputPath,
            NoPackageAnalysis = true
        });
});

Task("Deploy-OctopusDeploy")
    .IsDependentOn("Package-NuGet")
    .Does(() =>
{
    Information($"Octopus PUSH");
    OctoPush(
        Urls.OctopusServerUrl,
        EnvironmentVariable("OctopusApiKey"),
        GetFiles($"{packageOutputPath}/*.*"),
        new OctopusPushSettings
        {
            ReplaceExisting = true
        });    

    OctoCreateRelease(
        "Linker",
        new CreateReleaseSettings
        {
            Server = Urls.OctopusServerUrl,
            ApiKey = EnvironmentVariable("OctopusApiKey"),
            ReleaseNumber = packageVersion,
            DefaultPackageVersion = packageVersion,
            DeployTo = "Test",
            WaitForDeployment = true
        });
});


Task("Test")
    .IsDependentOn("Test-OpenCover");

RunTarget(target);
