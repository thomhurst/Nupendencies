using Microsoft.Extensions.Logging;
using TomLonghurst.Nupendencies.Contracts;
using TomLonghurst.Nupendencies.Extensions;
using TomLonghurst.Nupendencies.Options;

namespace TomLonghurst.Nupendencies.Services;

public class UnusedDependencyRemover : IUnusedDependencyRemover
{
    private readonly IPreviousResultsService _previousResultsService;
    private readonly IPackageVersionScanner _packageVersionScanner;
    private readonly ILogger<UnusedDependencyRemover> _logger;
    private readonly ISolutionBuilder _solutionBuilder;
    private readonly NupendenciesOptions _nupendenciesOptions;

    private readonly Func<ProjectPackage, bool>[] _packagesToNotRemoveRules = {
        s => s.Name.Contains("Microsoft.NET.Test", StringComparison.OrdinalIgnoreCase),
        s => s.Name.Contains("NUnit", StringComparison.OrdinalIgnoreCase),
        s => s.Name.Contains("coverlet", StringComparison.OrdinalIgnoreCase),
        s => s.Name.EndsWith(".Targets", StringComparison.OrdinalIgnoreCase),
        s => s.Name.Contains("Runtime", StringComparison.OrdinalIgnoreCase),
        s => s.Name.Contains("Analyzer", StringComparison.OrdinalIgnoreCase),
        s => s.Name.Contains("Analyser", StringComparison.OrdinalIgnoreCase),
        s => s.Name.Contains("CodeDom", StringComparison.OrdinalIgnoreCase),
        s => s.PackageReferenceTag.Metadata?.Any(m => m.Name == "OutputItemType") == true,
        s => s.IsConditional,
        s => s.Name.StartsWith("Microsoft.ApplicationInsights.Web", StringComparison.OrdinalIgnoreCase),
        s => s.Name.Contains("AspNet", StringComparison.OrdinalIgnoreCase),
        s => s.Name.StartsWith("Newtonsoft", StringComparison.OrdinalIgnoreCase),
        s => s.Name.StartsWith("System.Text.Json", StringComparison.OrdinalIgnoreCase),
        s => s.Name.Contains("Diagnostics", StringComparison.OrdinalIgnoreCase),
        s => s.Name.Contains("Sdk", StringComparison.OrdinalIgnoreCase),
        s => s.Name.Contains("Worker", StringComparison.OrdinalIgnoreCase),
        s => s.Name.StartsWith("Microsoft.Extensions", StringComparison.OrdinalIgnoreCase),
    };

    public UnusedDependencyRemover(IPreviousResultsService previousResultsService, 
        IPackageVersionScanner packageVersionScanner, 
        ILogger<UnusedDependencyRemover> logger, 
        ISolutionBuilder solutionBuilder,
        NupendenciesOptions nupendenciesOptions)
    {
        _previousResultsService = previousResultsService;
        _packageVersionScanner = packageVersionScanner;
        _logger = logger;
        _solutionBuilder = solutionBuilder;
        _nupendenciesOptions = nupendenciesOptions;
    }

    public async IAsyncEnumerable<DependencyRemovalResult> TryDeleteUnusedPackages(CodeRepository repository)
    {
        if (!_nupendenciesOptions.TryRemoveUnusedPackages)
        {
            yield break;
        }
        
        var allProjects = repository.AllProjects;

        foreach (var package in allProjects
                     .SelectMany(p => p.Packages)
                     .Where(packageReferenceItem => !_packagesToNotRemoveRules.Any(rule => rule(packageReferenceItem)))
                )
        {
            var name = package.Name;
            var projectPath = package.Project.ProjectPath;
            var version = package.OriginalVersion;

            if (!_previousResultsService.ShouldTryRemove(package))
            {
                _logger.LogDebug("Skipping removal of {Package} from {Project} due to it not being successful in a previous run", name, package.Project.Name);
                continue;
            }

            if (package.IsConditional)
            {
                continue;
            }

            package.Remove();

            if (await _packageVersionScanner.DowngradeDetected(package.Project, name, version))
            {
                UndoPackageRemoval(package);
                continue;
            }

            var projectsToBuild = package.GetProjectsToBuild();

            var build = await _solutionBuilder.BuildProjects(projectsToBuild);

            if (!build.IsSuccessful)
            {
                UndoPackageRemoval(package);
                continue;
            }

            _logger.LogInformation("Package {PackageName} was successfully removed from Project {ProjectPath}",
                name, projectPath);

            yield return new DependencyRemovalResult(true, name, package);
        }
    }

    private void UndoPackageRemoval(ProjectPackage package)
    {
        _logger.LogDebug("Package {PackageName} could not be removed from Project {ProjectName}",
            package.Name, package.Project.Name);

        _previousResultsService.WriteUnableToRemovePackageEntry(package);

        package.UndoRemove();
    }
}