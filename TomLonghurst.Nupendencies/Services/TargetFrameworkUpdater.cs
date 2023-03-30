using System.Collections.Immutable;
using Microsoft.Extensions.Logging;

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
    
    public async Task TryUpdateTargetFramework(CodeRepository repository)
    {
        var netCoreProjects = repository.AllProjects
            .Where(x => !x.IsMultiTargeted)
            .Where(x => x.TargetFramework.HasValue)
            .Where(x => x.IsNetCore)
            .ToList();

        if (!netCoreProjects.Any())
        {
            return;
        }
        
        foreach (var netCoreProject in netCoreProjects)
        {
            netCoreProject.TargetFramework.CurrentValue = LatestNetValue;
        }

        var projectsToBuild = netCoreProjects
            .SelectMany(p => p.GetUppermostProjectsReferencingThisProject())
            .ToImmutableHashSet();

        var solutionBuildResult = await _solutionBuilder.BuildProjects(projectsToBuild);
    
        var solutionBuiltSuccessfully = solutionBuildResult.IsSuccessful;
            
        if (!solutionBuiltSuccessfully)
        {
            netCoreProjects.ForEach(x => x.TargetFramework.Rollback());

            _logger.LogWarning(".NET Version Update from {OldVersion} to {LatestVersion} failed", netCoreProjects.First().TargetFramework.OriginalValue, LatestNetValue);
        }
        else
        {
            _logger.LogDebug(".NET Version Update from {OldVersion} to {LatestVersion} was successful", netCoreProjects.First().TargetFramework.OriginalValue, LatestNetValue);
        }
    }
}