using Microsoft.Extensions.Logging;
using TomLonghurst.Nupendencies.Abstractions.Extensions;
using TomLonghurst.Nupendencies.Abstractions.Models;
using TomLonghurst.Nupendencies.Contracts;

namespace TomLonghurst.Nupendencies.Services;

public class TargetFrameworkUpdater : ITargetFrameworkUpdater
{
    public static readonly string LatestNetValue = "net7.0";

    private readonly ISolutionBuilder _solutionBuilder;
    private readonly ILogger<TargetFrameworkUpdater> _logger;

    public TargetFrameworkUpdater(ISolutionBuilder solutionBuilder,
        ILogger<TargetFrameworkUpdater> logger)
    {
        _solutionBuilder = solutionBuilder;
        _logger = logger;
    }
    
    public async Task<TargetFrameworkUpdateResult> TryUpdateTargetFramework(CodeRepository repository)
    {
        var netCoreProjects = repository.AllProjects
            .Where(x => !x.IsMultiTargeted)
            .Where(x => x.TargetFramework.HasValue)
            .Where(x => x.IsNetCore)
            .ToList();

        if (!netCoreProjects.Any(NeedsUpdating))
        {
            return new TargetFrameworkUpdateResult(false, string.Empty, string.Empty);
        }
        
        foreach (var netCoreProject in netCoreProjects)
        {
            netCoreProject.TargetFramework.CurrentValue = LatestNetValue;
        }

        var projectsToBuild = netCoreProjects.SelectMany(x => x.Repository.AllProjects).GetProjectsToBuild();

        var solutionBuildResult = await _solutionBuilder.BuildProjects(projectsToBuild, false);
    
        var solutionBuiltSuccessfully = solutionBuildResult.IsSuccessful;

        var targetFrameworkOriginalValue = netCoreProjects.First().TargetFramework.OriginalValue;
        
        if (!solutionBuiltSuccessfully)
        {
            netCoreProjects.ForEach(x => x.TargetFramework.Rollback());

            _logger.LogWarning(".NET Version Update from {OldVersion} to {LatestVersion} failed", targetFrameworkOriginalValue, LatestNetValue);

            return new TargetFrameworkUpdateResult(false, targetFrameworkOriginalValue!, LatestNetValue);
        }

        _logger.LogDebug(".NET Version Update from {OldVersion} to {LatestVersion} was successful", targetFrameworkOriginalValue, LatestNetValue);
        return new TargetFrameworkUpdateResult(true, targetFrameworkOriginalValue!, LatestNetValue);
    }

    private static bool NeedsUpdating(Project x)
    {
        var currentVersionString = x.TargetFramework.CurrentValue!
            .Replace("netcoreapp", string.Empty)
            .Replace("net", string.Empty);
        
        var currentVersion = double.Parse(currentVersionString);
        
        var latestVersion = double.Parse(LatestNetValue.Replace("net", string.Empty)); 
        
        return currentVersion < latestVersion;
    }
}