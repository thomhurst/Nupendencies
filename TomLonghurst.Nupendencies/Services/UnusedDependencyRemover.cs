﻿using Microsoft.Extensions.Logging;
using TomLonghurst.Nupendencies.Abstractions.Extensions;
using TomLonghurst.Nupendencies.Abstractions.Models;
using TomLonghurst.Nupendencies.Contracts;
using TomLonghurst.Nupendencies.Options;

namespace TomLonghurst.Nupendencies.Services;

public class UnusedDependencyRemover : IUnusedDependencyRemover
{
    private readonly IPackageVersionScanner _packageVersionScanner;
    private readonly ILogger<UnusedDependencyRemover> _logger;
    private readonly ISolutionBuilder _solutionBuilder;
    private readonly NupendenciesOptions _nupendenciesOptions;

    private readonly Func<ProjectPackage, bool>[] _packagesToNotRemoveRules = {
        s => s.IsConditional,
        s => s.PackageReferenceTag.Metadata?.Any(m => m.Name.Contains("Assets", StringComparison.OrdinalIgnoreCase)) == true,
        s => s.Name.Contains("Microsoft.NET.Test", StringComparison.OrdinalIgnoreCase),
        s => s.Name.Contains("NUnit", StringComparison.OrdinalIgnoreCase),
        s => s.Name.Contains("coverlet", StringComparison.OrdinalIgnoreCase),
        s => s.Name.EndsWith(".Targets", StringComparison.OrdinalIgnoreCase),
        s => s.Name.Contains("Runtime", StringComparison.OrdinalIgnoreCase),
        s => s.Name.Contains("Analyzer", StringComparison.OrdinalIgnoreCase),
        s => s.Name.Contains("Analyser", StringComparison.OrdinalIgnoreCase),
        s => s.Name.Contains("CodeDom", StringComparison.OrdinalIgnoreCase),
        s => s.PackageReferenceTag.Metadata?.Any(m => m.Name.Contains("OutputItemType", StringComparison.OrdinalIgnoreCase)) == true,
        s => s.Name.StartsWith("Microsoft.ApplicationInsights.Web", StringComparison.OrdinalIgnoreCase),
        s => s.Name.Contains("AspNet", StringComparison.OrdinalIgnoreCase),
        s => s.Name.StartsWith("Newtonsoft", StringComparison.OrdinalIgnoreCase),
        s => s.Name.StartsWith("System.Text.Json", StringComparison.OrdinalIgnoreCase),
        s => s.Name.Contains("Diagnostics", StringComparison.OrdinalIgnoreCase),
        s => s.Name.Contains("Sdk", StringComparison.OrdinalIgnoreCase),
        s => s.Name.Contains("Worker", StringComparison.OrdinalIgnoreCase),
        s => s.Name.StartsWith("Microsoft.Extensions", StringComparison.OrdinalIgnoreCase),
    };

    public UnusedDependencyRemover(IPackageVersionScanner packageVersionScanner, 
        ILogger<UnusedDependencyRemover> logger, 
        ISolutionBuilder solutionBuilder,
        NupendenciesOptions nupendenciesOptions)
    {
        _packageVersionScanner = packageVersionScanner;
        _logger = logger;
        _solutionBuilder = solutionBuilder;
        _nupendenciesOptions = nupendenciesOptions;
    }

    public async IAsyncEnumerable<PackageRemovalResult> TryDeleteRedundantPackageReferences(CodeRepository repository)
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

            var projectsToBuild = package.Project.Repository.AllProjects.GetProjectsToBuild();

            var build = await _solutionBuilder.BuildProjects(projectsToBuild, false);

            if (!build.IsSuccessful)
            {
                UndoPackageRemoval(package);
                continue;
            }
            
            package.Tidy();

            _logger.LogInformation("Package {PackageName} was successfully removed from Project {ProjectPath}",
                name, projectPath);

            yield return new PackageRemovalResult(true, name, package);
        }
    }
    
    public async IAsyncEnumerable<ProjectRemovalResult> TryDeleteRedundantProjectReferences(CodeRepository repository)
    {
        if (!_nupendenciesOptions.TryRemoveUnusedProjects)
        {
            yield break;
        }
        
        var allProjects = repository.AllProjects;

        foreach (var childProject in allProjects
                     .SelectMany(p => p.Children)
                )
        {
            if (childProject.IsConditional)
            {
                continue;
            }

            childProject.RemoveReferenceTag();

            var projectsToBuild = childProject.Project.Repository.AllProjects.GetProjectsToBuild();

            var build = await _solutionBuilder.BuildProjects(projectsToBuild, false);

            _logger.LogDebug("Build failure after removing project {Project}: {Output}", childProject.Project.Name, build.OutputString);

            if (!build.IsSuccessful)
            {
                UndoProjectReferenceRemoval(childProject);
                continue;
            }
            
            childProject.Tidy();

            _logger.LogInformation("Project {ProjectName} was successfully removed from Project {ContaingProjectName}",
                childProject.Project.Name, childProject.ParentProject.Name);

            yield return new ProjectRemovalResult(true, childProject.Project.Name, childProject.ParentProject);
        }
    }

    private void UndoPackageRemoval(ProjectPackage package)
    {
        _logger.LogDebug("Package {PackageName} could not be removed from Project {ProjectName}",
            package.Name, package.Project.Name);
        
        package.UndoRemove();
    }
    
    private void UndoProjectReferenceRemoval(ChildProject childProject)
    {
        _logger.LogDebug("Package {ProjectName} could not be removed from Project {ContainingProjectName}",
            childProject.Project.Name, childProject.ParentProject.Name);
        
        childProject.UndoRemove();
    }
}