﻿using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using NuGet.Packaging.Core;
using Semver;
using TomLonghurst.Nupendencies.Abstractions.Extensions;
using TomLonghurst.Nupendencies.Abstractions.Models;
using TomLonghurst.Nupendencies.Clients;
using TomLonghurst.Nupendencies.Contracts;
using TomLonghurst.Nupendencies.Models;
using TomLonghurst.Nupendencies.Options;

namespace TomLonghurst.Nupendencies.Services;

public class DependencyUpdater : IDependencyUpdater
{
    private readonly ILogger<DependencyUpdater> _logger;
    private readonly NuGetClient _nuGetClient;
    private readonly ISolutionBuilder _solutionBuilder;
    private readonly NupendenciesOptions _nupendenciesOptions;

    public DependencyUpdater(ILogger<DependencyUpdater> logger,
        NuGetClient nuGetClient,
        ISolutionBuilder solutionBuilder,
        NupendenciesOptions nupendenciesOptions)
    {
        _logger = logger;
        _nuGetClient = nuGetClient;
        _solutionBuilder = solutionBuilder;
        _nupendenciesOptions = nupendenciesOptions;
    }

    public async Task<IList<PackageUpdateResult>> TryUpdatePackages(CodeRepository codeRepository)
    {
        var results = new List<PackageUpdateResult>();

        var allProjects = codeRepository.AllProjects;

        var packagesGrouped = allProjects
            .SelectMany(p => p.Packages)
            .GroupBy(x => x.Name)
            .ToList();

        var nugetDependencyDetails = await _nuGetClient.GetPackages(packagesGrouped.Select(x => x.Key));

        var updateAllResults = await TryUpdateAllPackagesSimultaneously(packagesGrouped, nugetDependencyDetails);
        if (updateAllResults.Any() && updateAllResults.All(x => x.UpdateBuiltSuccessfully))
        {
            _logger.LogInformation("Successfully updated all projects simultaneously");
            return updateAllResults;
        }

        _logger.LogInformation("Build errors - Falling back to updating packages one by one");

        var allNugetDependencies = nugetDependencyDetails.SelectMany(d => d.Dependencies).ToList();
        foreach (var nuGetPackageInformation in nugetDependencyDetails
                     .Where(x => x?.PackageName != null)
                     .OrderBy(npi => GetEfficientOrder(npi, allNugetDependencies)))
        {
            var tagsWithThisNugetPackage =
                packagesGrouped.FirstOrDefault(x => x.Key == nuGetPackageInformation.PackageName);

            if (tagsWithThisNugetPackage == null)
            {
                continue;
            }

            var packageReferencesList = tagsWithThisNugetPackage.ToList();

            var projectsToBuild = packageReferencesList.GetProjectsToBuild();

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

                var projectsToBuild = packages.GetProjectsToBuild();

                if (await TryUpdatePackages(projectsToBuild, packages, update.LatestVersionAttempted, results))
                {
                    successCount++;
                }
            }
        } while (successCount > 0);

        return results;
    }

    private bool ShouldUpdatePackage(string packageName, SemVersion originalVersion, string latestVersion)
    {
        if (!SemVersion.TryParse(latestVersion, SemVersionStyles.Any, out var latestSemVersion))
        {
            return false;
        }
        
        if (_nupendenciesOptions.ShouldUpdatePackagePredicate == null)
        {
            return true;
        }

        return _nupendenciesOptions.ShouldUpdatePackagePredicate.Invoke(new PackageUpdateModel(packageName, originalVersion, latestSemVersion));
    }

    private int GetEfficientOrder(NuGetPackageInformation nuGetPackageInformation,
        IEnumerable<PackageDependency> allNugetDependencies)
    {
        // Some packages containing references to other packages can't be updated first until the nested package has been updated, to avoid version downgrades
        // We should try to update dependencies first where they don't appear to depend on another
        if (!nuGetPackageInformation.Dependencies.Any())
        {
            return 0;
        }

        if (allNugetDependencies.Any(d => d.Id == nuGetPackageInformation.PackageName))
        {
            return 1;
        }

        return nuGetPackageInformation.Dependencies.Count;
    }

    private async Task<IList<PackageUpdateResult>> TryUpdateAllPackagesSimultaneously(List<IGrouping<string, ProjectPackage>> packagesGrouped, NuGetPackageInformation[] nugetDependencyDetails)
    {
        var packagesNeedingUpdate = GetPackagesNeedingUpdate(packagesGrouped, nugetDependencyDetails).ToArray();
        
        foreach (var updateablePackage in packagesNeedingUpdate)
        {
            updateablePackage.Packages.ForEach(p =>
                p.CurrentVersion = SemVersion.Parse(updateablePackage.NuGetPackageInformation.Version.ToNormalizedString(), SemVersionStyles.Any));
        }

        var buildResult = await _solutionBuilder.BuildProjects(packagesGrouped.SelectMany(x => x).GetProjectsToBuild(), false);

        if (!buildResult.IsSuccessful)
        {
            foreach (var projectPackage in packagesGrouped.SelectMany(x => x))
            {
                projectPackage.RollbackVersion();
            }

            return Array.Empty<PackageUpdateResult>();
        }

        return packagesNeedingUpdate.Select(x => new PackageUpdateResult
        {
            PackageName = x.NuGetPackageInformation.PackageName,
            Packages = x.Packages.ToHashSet(),
            LatestVersionAttempted = x.NuGetPackageInformation.Version.ToNormalizedString(),
            UpdateBuiltSuccessfully = true,
            PackageDowngradeDetected = false
        }).ToList();
    }

    private record UpdateablePackage(IEnumerable<ProjectPackage> Packages, NuGetPackageInformation NuGetPackageInformation);
    private IEnumerable<UpdateablePackage> GetPackagesNeedingUpdate(List<IGrouping<string, ProjectPackage>> packagesGrouped, NuGetPackageInformation[] nugetDependencyDetails)
    {
        foreach (var projectPackages in packagesGrouped)
        {
            foreach (var nuGetPackageInformation in nugetDependencyDetails)
            {
                if (!string.Equals(projectPackages.Key, nuGetPackageInformation.PackageName,
                        StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }

                if (!VersionsNeedsUpdating(nuGetPackageInformation.PackageName, nuGetPackageInformation.Version.ToNormalizedString(),
                        projectPackages.Min(x => x.OriginalVersion)!))
                {
                    continue;
                }

                yield return new UpdateablePackage(projectPackages, nuGetPackageInformation);
            }
        }
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
            .Where(v => VersionsNeedsUpdating(v.Name, latestVersion, v.OriginalVersion))
            .ToList();

        if (!packagesNeedingUpdating.Any())
        {
            _logger.LogDebug("No out of date versions found for package {Package}", packageName);
            return false;
        }

        var originalPackageVersion = packagesNeedingUpdating.First().OriginalVersion;

        packagesNeedingUpdating.ForEach(p => p.CurrentVersion = SemVersion.Parse(latestVersion, SemVersionStyles.Any));

        var solutionBuildResult = await _solutionBuilder.BuildProjects(projectsToBuild, false);

        var solutionBuiltSuccessfully = solutionBuildResult.IsSuccessful;

        if (!solutionBuiltSuccessfully)
        {
            packagesNeedingUpdating.ForEach(p => p.RollbackVersion());

            _logger.LogWarning("Package {PackageName} upgrade from {OldVersion} to {LatestVersion} failed", packageName,
                originalPackageVersion, latestVersion);
            _logger.LogDebug("Package {PackageName} build output is: {BuildOutput}", packageName,
                FormatOutput(solutionBuildResult));
        }
        else
        {
            _logger.LogInformation("Package {PackageName} upgrade from {OldVersion} to {LatestVersion} was successful",
                packageName, originalPackageVersion, latestVersion);
        }

        UpdateInfo(latestVersion, packageUpdateResults, packageName, packagesNeedingUpdating, solutionBuildResult);

        return solutionBuiltSuccessfully;
    }

    private string FormatOutput(SolutionBuildResult solutionBuildResult)
    {
        return string.Join(Environment.NewLine, solutionBuildResult.OutputErrors ?? new List<string>()) +
               Environment.NewLine + solutionBuildResult.OutputString;
    }

    private bool VersionsNeedsUpdating(string packageName, string latestVersionString, SemVersion originalVersion)
    {
        if (!SemVersion.TryParse(latestVersionString, SemVersionStyles.Any, out var latestVersion))
        {
            return false;
        }

        if (originalVersion == latestVersion)
        {
            return false;
        }

        var compareSortOrder = SemVersion.CompareSortOrder(originalVersion, latestVersion);

        if (compareSortOrder >= 0)
        {
            return false;
        }
        
        return ShouldUpdatePackage(packageName, originalVersion, latestVersion?.ToString() ?? "");
    }

    private static void UpdateInfo(string latestVersion, ICollection<PackageUpdateResult> packageUpdateResults,
        string packageName, List<ProjectPackage> packages,
        SolutionBuildResult solutionBuildResult)
    {
        var existingElement = packageUpdateResults.FirstOrDefault(x => x.PackageName == packageName);

        if (existingElement != null)
        {
            existingElement.Packages.AddRange(packages);
            existingElement.UpdateBuiltSuccessfully = solutionBuildResult.IsSuccessful;
            existingElement.PackageDowngradeDetected = solutionBuildResult.DetectedDowngrade;
            return;
        }

        packageUpdateResults.Add(new PackageUpdateResult
        {
            PackageName = packageName,
            LatestVersionAttempted = latestVersion,
            Packages = packages.ToHashSet(),
            UpdateBuiltSuccessfully = solutionBuildResult.IsSuccessful,
            PackageDowngradeDetected = solutionBuildResult.DetectedDowngrade
        });
    }
}