using Microsoft.Extensions.Logging;
using TomLonghurst.Nupendencies.Contracts;
using TomLonghurst.Nupendencies.Extensions;

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

        if (!netCoreProjects.Any())
        {
            return new TargetFrameworkUpdateResult(false, string.Empty, string.Empty);
        }
        
        foreach (var netCoreProject in netCoreProjects)
        {
            netCoreProject.TargetFramework.CurrentValue = LatestNetValue;
        }

        var projectsToBuild = netCoreProjects.GetProjectsToBuild();

        var solutionBuildResult = await _solutionBuilder.BuildProjects(projectsToBuild);
    
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
}