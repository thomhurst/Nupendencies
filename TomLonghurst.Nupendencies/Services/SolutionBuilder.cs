using System.Collections.Immutable;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.Extensions.Logging;
using TomLonghurst.Nupendencies.Abstractions.Models;
using TomLonghurst.Nupendencies.Contracts;
using TomLonghurst.Nupendencies.Models;
using TomLonghurst.Nupendencies.Options;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace TomLonghurst.Nupendencies.Services;

public class SolutionBuilder : ISolutionBuilder
{
    private readonly NupendenciesOptions _nupendenciesOptions;
    private readonly ILogger<SolutionBuilder> _logger;
    private readonly INetSdkProvider _netSdkProvider;
    private readonly string _azureArtifactsCredentialsJson;

    public SolutionBuilder(NupendenciesOptions nupendenciesOptions, ILogger<SolutionBuilder> logger, INetSdkProvider netSdkProvider)
    {
        _nupendenciesOptions = nupendenciesOptions;
        _logger = logger;
        _netSdkProvider = netSdkProvider;

        _azureArtifactsCredentialsJson = JsonSerializer.Serialize(new AzureArtifactsCredentials(_nupendenciesOptions.PrivateNugetFeedOptions
            .Select(x => new EndpointCredential(x.SourceUrl, x.Username, x.PatToken))
            .ToList()));
    }
    
    public async Task<SolutionBuildResult> BuildProjects(ImmutableHashSet<Project> projects, string target = "build")
    {
        var results = await Build(projects, target);

        var isSuccessful = CheckIsSuccessful(results);

        if (!isSuccessful)
        {
            foreach (var result in results)
            {
                _logger.Log(LogLevel.Debug, "Build Error: {Output}", result.Output);
            }
        }
        
        return new SolutionBuildResult
        {
            IsSuccessful = isSuccessful,
            OutputErrors = results.Select(x => x.Output).ToList()
        };
    }

    private static bool CheckIsSuccessful(IEnumerable<ProjectBuildResult> results)
    {
        return results.All(rw => rw.ExitCode == 0);
    }
    
    private async Task<IList<ProjectBuildResult>> Build(ImmutableHashSet<Project> projectsToBuild, string target = "build")
    {
        var results = new List<ProjectBuildResult>();

        foreach (var projectToBuild in projectsToBuild.Distinct())
        {
            // Fail fast
            if (results.Any(x => x.ExitCode != 0))
            {
                return results;
            }

            if (ShouldBuildUsingLegacyMsBuild(projectToBuild))
            {
                results.Add(await BuildUsingLegacyMsBuild(projectToBuild, target));
            }
            else
            {
                results.Add(await BuildUsingDotnet(projectToBuild, target));
            }
        }

        return results;
    }

    private bool ShouldBuildUsingLegacyMsBuild(Project projectToBuild)
    {
        if (string.IsNullOrEmpty(projectToBuild.ProjectRootElement.Sdk))
        {
            return true;
        }

        var referenceElements = projectToBuild.ProjectRootElement.Items.Where(x => x.ItemType == "Reference");
        if (referenceElements.Any())
        {
            return true;
        }

        var childProjects = projectToBuild.Children;

        return childProjects.Any(ShouldBuildUsingLegacyMsBuild);
    }

    private async Task<ProjectBuildResult> BuildUsingDotnet(Project projectToBuild, string target = "build")
    {
        var sdk = await _netSdkProvider.GetLatestDotnetSdk();

        if (string.IsNullOrEmpty(sdk?.Directory))
        {
            return new ProjectBuildResult
            {
                ExitCode = -1,
                Output = "No dotnet.exe found. Cannot build this project."
            };
        }
        
        if (string.IsNullOrEmpty(projectToBuild.ProjectPath))
        {
            return new ProjectBuildResult
            {
                ExitCode = -1,
                Output = "No project found to build"
            };
        }
        
        var result = await Cli.Wrap("dotnet")
            .WithWorkingDirectory(sdk.Directory)
            .WithArguments($"{target} \"{projectToBuild.ProjectPath}\" --configuration Release /p:WarningLevel=0 /p:CheckEolTargetFramework=false")
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

        return new ProjectBuildResult
        {
            Output = result.StandardOutput + Environment.NewLine + result.StandardError,
            ExitCode = result.ExitCode
        };
    }

    private async Task<ProjectBuildResult> BuildUsingLegacyMsBuild(Project projectToBuild, string target = "build")
    {
        var sdk = await _netSdkProvider.GetLatestMSBuild();

        if (string.IsNullOrEmpty(sdk?.Directory))
        {
            return new ProjectBuildResult
            {
                ExitCode = -1,
                Output = "No MSBuild.exe found. Cannot build this project."
            };
        }
        
        if (string.IsNullOrEmpty(projectToBuild.ProjectPath))
        {
            return new ProjectBuildResult
            {
                ExitCode = -1,
                Output = "No project found to build"
            };
        }
        
        var result = await Cli.Wrap(Path.Combine(sdk.Directory, "MSBuild.exe"))
                .WithWorkingDirectory(Path.GetDirectoryName(projectToBuild.ProjectPath))
                .WithArguments($"\"{Path.GetFileName(projectToBuild.ProjectPath)}\" /restore -t:{target} /p:Configuration=Release /p:WarningLevel=0 /p:CheckEolTargetFramework=false")
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

        return new ProjectBuildResult
        {
            Output = result.StandardOutput + Environment.NewLine + result.StandardError,
            ExitCode = result.ExitCode
        };
    }
}