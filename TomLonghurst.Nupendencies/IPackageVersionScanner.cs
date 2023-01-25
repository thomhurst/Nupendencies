using System.Runtime.InteropServices;
using System.Text.Json;
using CliWrap;
using CliWrap.Buffered;
using NuGet.ProjectModel;
using Semver;
using TomLonghurst.EnumerableAsyncProcessor.Extensions;
using TomLonghurst.Nupendencies.Clients;
using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies;

public interface IPackageVersionScanner
{
    Task<DependencyGraphSpec> GenerateDependencyGraph(string projectPath);

    Task<bool> DowngradeDetected(RepositoryProjectTree repositoryProjectTree, string projectPath,
        string packageName, string version);
}

public class PackageVersionScanner : IPackageVersionScanner
{
    private readonly INetSdkProvider _netSdkProvider;
    private readonly NuGetClient _nuGetClient;
    private readonly string? _azureArtifactsCredentialsJson;

    public PackageVersionScanner(INetSdkProvider netSdkProvider, 
        NupendenciesOptions nupendenciesOptions,
        NuGetClient nuGetClient)
    {
        _netSdkProvider = netSdkProvider;
        _nuGetClient = nuGetClient;
        _azureArtifactsCredentialsJson = JsonSerializer.Serialize(new AzureArtifactsCredentials(nupendenciesOptions.PrivateNugetFeedOptions
            .Select(x => new EndpointCredential(x.SourceUrl, x.Username, x.PatToken))
            .ToList()));
    }
    
    public async Task<DependencyGraphSpec> GenerateDependencyGraph(string projectPath)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        
        try
        {
            var sdk = await _netSdkProvider.GetLatestMSBuild();

            if (string.IsNullOrEmpty(sdk?.Directory))
            {
                return null;
            }
        
            if (string.IsNullOrEmpty(projectPath))
            {
                return null;
            }
        
            var result = await Cli.Wrap(Path.Combine(sdk.Directory, "MSBuild.exe"))
                .WithWorkingDirectory(Path.GetDirectoryName(projectPath))
                .WithArguments($"\"{Path.GetFileName(projectPath)}\" /t:GenerateRestoreGraphFile /p:RestoreGraphOutputPath={tempFile}")
                .WithEnvironmentVariables(new Dictionary<string, string?>
                {
                    ["VSS_NUGET_EXTERNAL_FEED_ENDPOINTS"] = _azureArtifactsCredentialsJson,
                    ["MSBuildLoadMicrosoftTargetsReadOnly"] = "true",
                    ["MsBuildExtensionPath"] = null,
                    ["MsBuildSDKsPath"] = null,
                    ["MSBUILD_EXE_PATH"] = null
                })
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync();
            
            if (result.ExitCode != 0)
            {
                return new DependencyGraphSpec(true);
            }

            return DependencyGraphSpec.Load(tempFile);
        }
        finally
        {
            if(File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    public async Task<bool> DowngradeDetected(RepositoryProjectTree repositoryProjectTree, string projectPath,
        string packageName, string versionRemoved)
    {
        if(!SemVersion.TryParse(versionRemoved, SemVersionStyles.Any, out var version))
        {
            // We're going to struggle to compare this. If a well established version library can't parse it, we're not going to be able to do much better.
            return true;
        }
        
        var upperProjects = repositoryProjectTree.GetUppermostProjectsReferencingThisProject(projectPath);

        var newDependencyGraph = await upperProjects
            .Where(x => File.Exists(x.AbsoluteFilePath))
            .ToAsyncProcessorBuilder()
            .SelectAsync(x => GenerateDependencyGraph(x.AbsoluteFilePath))
            .ProcessInParallel();

        var newDirectDependencies = newDependencyGraph
            .Where(x => x != null)
            .SelectMany(x => x.Projects)
            .Where(x => x != null)
            .SelectMany(x => x.TargetFrameworks)
            .Where(x => x != null)
            .SelectMany(x => x.Dependencies)
            .Where(x => x != null)
            .ToList();

        var newDirectDependenciesOfThisPackage = newDirectDependencies
            .Where(x => x.Name == packageName)
            .ToList();

        var newIndirectDependencies = (await newDirectDependencies
            .GroupBy(x => x.Name)
            .Select(x => x.MaxBy(p => p.LibraryRange.VersionRange.MinVersion))
            .ToAsyncProcessorBuilder()
            .SelectAsync(x => _nuGetClient.GetPackage(x.Name, x.LibraryRange.VersionRange.MinVersion.ToFullString()))
            .ProcessInParallel())
            .Where(x => x != null)
            .SelectMany(x => x!.Dependencies)
            .Where(x => x != null)
            .ToList();
        
        var newIndirectDependenciesOfThisPackage = newIndirectDependencies
            .Where(x => x.Id == packageName)
            .ToList();

        if (!newDirectDependenciesOfThisPackage.Any() && !newIndirectDependenciesOfThisPackage.Any())
        {
            // This should be an un-consumed package able to be removed
            return false;
        }

        if (newDirectDependenciesOfThisPackage.Any(x => x.LibraryRange?.VersionRange?.MinVersion?.Version 
                                                        >= version?.ToVersion()))
        {
            return false;
        }

        if (newIndirectDependenciesOfThisPackage.Any(x => x.VersionRange?.MinVersion?.Version 
                                                          >= version?.ToVersion()))
        {
            return false;
        }

        return true;
    }
}

