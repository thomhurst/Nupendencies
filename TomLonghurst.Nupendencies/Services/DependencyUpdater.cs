using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.Common;
using Semver;
using TomLonghurst.Nupendencies.Clients;
using TomLonghurst.Nupendencies.Contracts;
using TomLonghurst.Nupendencies.Extensions;
using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Services;

public class DependencyUpdater : IDependencyUpdater
{
    private readonly ILogger<DependencyUpdater> _logger;
    private readonly NuGetClient _nuGetClient;
    private readonly ISolutionBuilder _solutionBuilder;

    public DependencyUpdater(ILogger<DependencyUpdater> logger,
        NuGetClient nuGetClient,
        ISolutionBuilder solutionBuilder)
    {
        _logger = logger;
        _nuGetClient = nuGetClient;
        _solutionBuilder = solutionBuilder;
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
        if (updateAllResults.All(x => x.UpdateBuiltSuccessfully))
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

    private async Task<IList<PackageUpdateResult>> TryUpdateAllPackagesSimultaneously(List<IGrouping<string, ProjectPackage>> packagesGrouped, NuGetPackageInformation[] nugetDependencyDetails)
    {
        var packagesNeedingUpdate = GetPackagesNeedingUpdate(packagesGrouped, nugetDependencyDetails).ToArray();
        
        foreach (var updateablePackage in packagesNeedingUpdate)
        {
            updateablePackage.Packages.ForEach(p =>
                p.CurrentVersion = updateablePackage.NuGetPackageInformation.Version.ToNormalizedString());
        }

        var buildResult = await _solutionBuilder.BuildProjects(packagesGrouped.SelectMany(x => x).GetProjectsToBuild());

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

                if (!VersionsNeedsUpdating(nuGetPackageInformation.Version.ToNormalizedString(),
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