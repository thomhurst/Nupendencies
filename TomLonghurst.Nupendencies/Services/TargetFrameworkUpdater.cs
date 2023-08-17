using Microsoft.Extensions.Logging;
using TomLonghurst.Nupendencies.Abstractions.Extensions;
using TomLonghurst.Nupendencies.Abstractions.Models;
using TomLonghurst.Nupendencies.Contracts;
using TomLonghurst.Nupendencies.Options;

namespace TomLonghurst.Nupendencies.Services;

public class TargetFrameworkUpdater : ITargetFrameworkUpdater
{
    private readonly ISolutionBuilder _solutionBuilder;
    private readonly ILogger<TargetFrameworkUpdater> _logger;
    private readonly NupendenciesOptions _nupendenciesOptions;

    public TargetFrameworkUpdater(ISolutionBuilder solutionBuilder,
        ILogger<TargetFrameworkUpdater> logger,
        NupendenciesOptions nupendenciesOptions)
    {
        _solutionBuilder = solutionBuilder;
        _logger = logger;
        _nupendenciesOptions = nupendenciesOptions;
    }
    
    public async Task<TargetFrameworkUpdateResult> TryUpdateTargetFramework(CodeRepository repository)
    {
        if (string.IsNullOrWhiteSpace(_nupendenciesOptions.TryUpdateTargetFrameworkTo))
        {
            return new TargetFrameworkUpdateResult(false, string.Empty, string.Empty);
        }
        
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
            netCoreProject.TargetFramework.CurrentValue = _nupendenciesOptions.TryUpdateTargetFrameworkTo;
        }

        var projectsToBuild = netCoreProjects.SelectMany(x => x.Repository.AllProjects).GetProjectsToBuild();

        var solutionBuildResult = await _solutionBuilder.BuildProjects(projectsToBuild, false);
    
        var solutionBuiltSuccessfully = solutionBuildResult.IsSuccessful;

        var targetFrameworkOriginalValue = netCoreProjects.First().TargetFramework.OriginalValue;
        
        if (!solutionBuiltSuccessfully)
        {
            netCoreProjects.ForEach(x => x.TargetFramework.Rollback());

            _logger.LogWarning(".NET Version Update from {OldVersion} to {LatestVersion} failed", targetFrameworkOriginalValue, _nupendenciesOptions.TryUpdateTargetFrameworkTo);

            return new TargetFrameworkUpdateResult(false, targetFrameworkOriginalValue!, _nupendenciesOptions.TryUpdateTargetFrameworkTo);
        }

        _logger.LogDebug(".NET Version Update from {OldVersion} to {LatestVersion} was successful", targetFrameworkOriginalValue, _nupendenciesOptions.TryUpdateTargetFrameworkTo);
        return new TargetFrameworkUpdateResult(true, targetFrameworkOriginalValue!, _nupendenciesOptions.TryUpdateTargetFrameworkTo);
    }

    private bool NeedsUpdating(Project x)
    {
        try
        {
            var currentVersionString = x.TargetFramework.CurrentValue!
                .Replace("netcoreapp", string.Empty)
                .Replace("net", string.Empty);
        
            var currentVersion = double.Parse(currentVersionString);
        
            var newVersion = double.Parse(_nupendenciesOptions.TryUpdateTargetFrameworkTo!.Replace("net", string.Empty)); 
        
            return currentVersion < newVersion;
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Error parsing Target Framework");
            return false;
        }
    }
}