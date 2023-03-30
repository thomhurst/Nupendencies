﻿using System.Collections.Immutable;
using System.Xml.Linq;
using Microsoft.Build.Construction;
using Microsoft.Extensions.Logging;
using Semver;
using TomLonghurst.Nupendencies.Clients;
using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Services;

public class CodeRepositoryUpdater : ICodeRepositoryUpdater
{
    private readonly NuGetClient _nuGetClient;
    private readonly ITargetFrameworkUpdater _targetFrameworkUpdater;
    private readonly IUnusedDependencyRemover _unusedDependencyRemover;
    private readonly ISolutionBuilder _solutionBuilder;
    private readonly ILogger<CodeRepositoryUpdater> _logger;

    public CodeRepositoryUpdater(NuGetClient nuGetClient,
        ITargetFrameworkUpdater targetFrameworkUpdater,
        IUnusedDependencyRemover unusedDependencyRemover,
        ISolutionBuilder solutionBuilder,
        ILogger<CodeRepositoryUpdater> logger,
        IPreviousResultsService previousResultsService,
        IPackageVersionScanner packageVersionScanner)
    {
        _nuGetClient = nuGetClient;
        _targetFrameworkUpdater = targetFrameworkUpdater;
        _unusedDependencyRemover = unusedDependencyRemover;
        _solutionBuilder = solutionBuilder;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PackageUpdateResult>> UpdateRepository(CodeRepository repository)
    {
        _logger.LogInformation("Project Tree: {ProjectTree}", repository);

        await _targetFrameworkUpdater.TryUpdateTargetFramework(repository);
        
        var deletionResults = await _unusedDependencyRemover.TryDeleteUnusedPackages(repository).ToListAsync();
        var updateResults = await TryUpdatePackages(repository);

        await UpdateWebConfigFiles(repository);
        
        return deletionResults.Concat(updateResults).ToList();
    }

    private async Task UpdateWebConfigFiles(CodeRepository codeRepository)
    {
        foreach (var webConfigFile in Directory
                     .GetFiles(codeRepository.RepositoryPath, "Web.config", SearchOption.AllDirectories)
                     .Distinct())
        {
            TryRegenerateBindings(webConfigFile);
        }

        // One last build in-case we need to generate binding redirects etc
        var upperMostProjects = codeRepository.AllProjects
            .SelectMany(project => project.GetUppermostProjectsReferencingThisProject())
            .ToImmutableHashSet();
        
        await _solutionBuilder.BuildProjects(upperMostProjects, "clean");
        await _solutionBuilder.BuildProjects(upperMostProjects);
    }

    private void TryRegenerateBindings(string webConfigFile)
    {
        try
        {
            var xdoc = XDocument.Load(webConfigFile);

            xdoc.Descendants("assemblyBinding")
                .ToList()
                .ForEach(x => x.Remove());

            xdoc.Save(webConfigFile);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error regenerating bindings");
        }
    }

    private async Task<List<PackageUpdateResult>> TryUpdatePackages(CodeRepository codeRepository)
    {
        var results = new List<PackageUpdateResult>();

        var allProjects = codeRepository.AllProjects;
        
        var packagesGrouped = allProjects
            .SelectMany(p => p.Packages)
            .GroupBy(x => x.Name)
            .ToList();

        var nugetDependencyDetails = await _nuGetClient.GetPackages(packagesGrouped.Select(x => x.Key));

        if (TryUpdateAllPackagesSimultaneously(nugetDependencyDetails, out var updateAllResults))
        {
            _logger.LogInformation("Successfully updated all projects simultaneously");
            return updateAllResults;
        }
        
        _logger.LogInformation("Build errors - Falling back to updating packages one by one");
        
        foreach (var nuGetPackageInformation in nugetDependencyDetails
                     .Where(x => x?.PackageName != null)
                     .OrderBy(x => x.PackageName))
        {
            var tagsWithThisNugetPackage =
                packagesGrouped.FirstOrDefault(x => x.Key == nuGetPackageInformation.PackageName);

            if (tagsWithThisNugetPackage == null)
            {
                continue;
            }

            var packageReferencesList = tagsWithThisNugetPackage.ToList();
            
            var projectsToBuild = packageReferencesList
                .SelectMany(x => x.Project.GetUppermostProjectsReferencingThisProject())
                .ToImmutableHashSet();

            await TryUpdatePackages(projectsToBuild, packageReferencesList,
                nuGetPackageInformation.Version.ToNormalizedString(),
                results);
        }

        // Some packages might not install initially because they depended on a newer version of another package first
        // So we can retry again, until all previously failed packages fail, so we know none are going to be successful again.
        int successCount;
        do
        {
            successCount = 0;
            foreach (var update in results.Where(x => x.PackageDowngradeDetected))
            {
                var packages = packagesGrouped.First(x => x.Key == update.PackageName).ToList();

                var projectsToBuild = packages
                    .SelectMany(x => x.Project.GetUppermostProjectsReferencingThisProject())
                    .ToImmutableHashSet();

                if (await TryUpdatePackages(projectsToBuild, packages, update.NewPackageVersion, results))
                {
                    successCount++;
                }
            }
        } while (successCount > 0);
        
        return results;
    }

    private async Task<bool> TryUpdatePackages(ImmutableHashSet<Project> projectsToBuild,
        List<ProjectPackage> packages, string latestVersion,
        List<PackageUpdateResult> packageUpdateResults)
        {
            if (!packages.Any())
            {
                return false;
            }

            var packageName = packages.First().Name;
            
            if (packages.Any(x => x.IsConditional))
            {
                // Need to think about logic for this.
                // Different frameworks need different versions.
                return false;
            }

            var packagesNeedingUpdating = packages
                .Where(v => VersionsNeedsUpdating(latestVersion, v.OriginalVersion))
                .ToList();

            if (!packagesNeedingUpdating.Any())
            {
                _logger.LogDebug("No out of date versions found for package {Package}", packageName);
                return false;
            }

            var originalPackageVersion = packagesNeedingUpdating.First().OriginalVersion;

            packagesNeedingUpdating.ForEach(p => p.CurrentVersion = latestVersion);

            var solutionBuildResult = await _solutionBuilder.BuildProjects(projectsToBuild);

            var solutionBuiltSuccessfully = solutionBuildResult.IsSuccessful;
            
            if (!solutionBuiltSuccessfully)
            {
                packagesNeedingUpdating.ForEach(p => p.RollbackVersion());

                _logger.LogWarning("Package {PackageName} upgrade from {OldVersion} to {LatestVersion} failed", packageName, originalPackageVersion, latestVersion);
                _logger.LogDebug("Package {PackageName} build output is: {BuildOutput}", packageName, FormatOutput(solutionBuildResult));
            }
            else
            {
                _logger.LogInformation("Package {PackageName} upgrade from {OldVersion} to {LatestVersion} was successful", packageName, originalPackageVersion, latestVersion);
            }
        
            UpdateInfo(latestVersion, packageUpdateResults, packageName, packagesNeedingUpdating, solutionBuildResult);

            return solutionBuiltSuccessfully;
        }

    private string FormatOutput(SolutionBuildResult solutionBuildResult)
    {
        return string.Join(Environment.NewLine, solutionBuildResult.OutputErrors ?? new List<string>()) +
               Environment.NewLine + solutionBuildResult.OutputString;
    }

    private static bool VersionsNeedsUpdating(string latestVersionString, SemVersion originalVersion)
    {
        if (!SemVersion.TryParse(latestVersionString, SemVersionStyles.Any, out var latestVersion))
        {
            return false;
        }
        
        if (originalVersion == latestVersion)
        {
            return false;
        }

        return originalVersion < latestVersion;
    }

    private static void UpdateInfo(string latestVersion, ICollection<PackageUpdateResult> packageUpdateResults, string packageName, List<ProjectPackage> packages,
            SolutionBuildResult solutionBuildResult)
        {
            var existingElement = packageUpdateResults.FirstOrDefault(x => x.PackageName == packageName);

            var oldVersion = packages.First().OriginalVersion;
            
            if (existingElement != null)
            {
                existingElement.NewPackageVersion = latestVersion;
                existingElement.OriginalPackageVersion = oldVersion.ToString();
                existingElement.UpdateBuiltSuccessfully = solutionBuildResult.IsSuccessful;
                existingElement.PackageDowngradeDetected = solutionBuildResult.DetectedDowngrade;
                existingElement.FileLines = solutionBuildResult.IsSuccessful
                    ? new List<string>()
                    : GetFileLineLocations(packages);

                return;
            }
        
            packageUpdateResults.Add(new PackageUpdateResult
            {
                PackageName = packageName,
                NewPackageVersion = latestVersion,
                OriginalPackageVersion = oldVersion.ToString(),
                UpdateBuiltSuccessfully = solutionBuildResult.IsSuccessful,
                PackageDowngradeDetected = solutionBuildResult.DetectedDowngrade,
                FileLines = solutionBuildResult.IsSuccessful ? new List<string>() : GetFileLineLocations(packages)
            });
        }

        private static List<string> GetFileLineLocations(IEnumerable<ProjectPackage> packageElementXmlWrappers)
        {
            return packageElementXmlWrappers
                .Select(x => Path.GetFileName(x.PackageReferenceTag.ContainingProject.Location.File) + $" | Line: {x.PackageReferenceTag.Location.Line}")
                .ToList();
        }
}