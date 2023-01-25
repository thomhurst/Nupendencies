using System.Xml.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;
using Microsoft.Extensions.Logging;
using NuGet.LibraryModel;
using Semver;
using TomLonghurst.EnumerableAsyncProcessor.Extensions;
using TomLonghurst.Nupendencies.Clients;
using TomLonghurst.Nupendencies.Models;
using Project = Microsoft.Build.Evaluation.Project;

namespace TomLonghurst.Nupendencies.Services;

public class SolutionUpdater : ISolutionUpdater
{
    private const string LatestNetValue = "net7.0";
    
    private readonly NuGetClient _nuGetClient;
    private readonly ISolutionBuilder _solutionBuilder;
    private readonly IRepositoryTreeGenerator _repositoryTreeGenerator;
    private readonly ILogger<SolutionUpdater> _logger;
    private readonly IPreviousResultsService _previousResultsService;
    private readonly IPackageVersionScanner _packageVersionScanner;

    private readonly Func<ProjectItemElement, bool>[] _packagesToNotRemoveRules = {
        s => s.Include.Contains("Microsoft.NET.Test", StringComparison.OrdinalIgnoreCase),
        s => s.Include.Contains("NUnit", StringComparison.OrdinalIgnoreCase),
        s => s.Include.Contains("coverlet", StringComparison.OrdinalIgnoreCase),
        s => s.Include.EndsWith(".Targets", StringComparison.OrdinalIgnoreCase),
        s => s.Include.Contains("Runtime", StringComparison.OrdinalIgnoreCase),
        s => s.Include.Contains("Analyzer", StringComparison.OrdinalIgnoreCase),
        s => s.Include.Contains("Analyser", StringComparison.OrdinalIgnoreCase),
        s => s.Include.Contains("CodeDom", StringComparison.OrdinalIgnoreCase),
        s => s.Metadata?.Any(m => m.Name == "OutputItemType") == true,
        //s => s.Metadata?.Any(m => m.Name.Contains("Assets")) == true,
        s => !string.IsNullOrEmpty(s.Condition),
        s => !string.IsNullOrEmpty(s.Parent.Condition),
        s => s.Include.StartsWith("Microsoft.ApplicationInsights.Web", StringComparison.OrdinalIgnoreCase),
        s => s.Include.Contains("AspNet", StringComparison.OrdinalIgnoreCase),
        s => s.Include.StartsWith("Newtonsoft", StringComparison.OrdinalIgnoreCase),
        s => s.Include.StartsWith("System.Text.Json", StringComparison.OrdinalIgnoreCase),
        s => s.Include.Contains("Diagnostics", StringComparison.OrdinalIgnoreCase),
        s => s.Include.Contains("Sdk", StringComparison.OrdinalIgnoreCase),
        s => s.Include.Contains("Worker", StringComparison.OrdinalIgnoreCase),
        s => s.Include.StartsWith("Microsoft.Extensions", StringComparison.OrdinalIgnoreCase),
    };

    public SolutionUpdater(NuGetClient nuGetClient,
        ISolutionBuilder solutionBuilder,
        IRepositoryTreeGenerator repositoryTreeGenerator,
        ILogger<SolutionUpdater> logger,
        IPreviousResultsService previousResultsService,
        IPackageVersionScanner packageVersionScanner)
    {
        _nuGetClient = nuGetClient;
        _solutionBuilder = solutionBuilder;
        _repositoryTreeGenerator = repositoryTreeGenerator;
        _logger = logger;
        _previousResultsService = previousResultsService;
        _packageVersionScanner = packageVersionScanner;
    }

    public async Task<IReadOnlyList<PackageUpdateResult>> UpdateSolutions(string[] solutionPaths)
    {
        //await TryUpdateNetVersion(solutionPaths);

        var allProjectsInAllSolutionsInRepository =
            solutionPaths.Select(SolutionFile.Parse)
                .SelectMany(s => s.ProjectsInOrder)
                .Where(p => p.AbsolutePath.EndsWith(".csproj"))
                .Where(p => File.Exists(p.AbsolutePath))
                .Select(p => ProjectRootElement.Open(p.AbsolutePath))
                .DistinctBy(p => p.Location.File)
                .ToArray();

        var projectTree = await _repositoryTreeGenerator.Generate(allProjectsInAllSolutionsInRepository);

        _logger.LogInformation("Project Tree: {ProjectTree}", projectTree);

        var deletionResults = await TryDeleteUnusedPackages(projectTree);
        var updateResults = await TryUpdatePackages(projectTree);

        await UpdateWebConfigFiles(solutionPaths, projectTree);
        
        return deletionResults.Concat(updateResults).ToList();
    }

    private async Task UpdateWebConfigFiles(string[] solutionPaths, RepositoryProjectTree projectTree)
    {
        foreach (var webConfigFile in solutionPaths.Select(p => new FileInfo(p).Directory.FullName)
                     .SelectMany(d => Directory.GetFiles(d, "Web.config", SearchOption.AllDirectories))
                     .Distinct())
        {
            TryRegenerateBindings(webConfigFile);
        }
        
        // One last build in-case we need to generate binding redirects etc
        var upperMostProjects = projectTree.AllProjects
            .SelectMany(x => projectTree.GetUppermostProjectsReferencingThisProject(x.AbsoluteFilePath))
            .Select(x => x.AbsoluteFilePath)
            .Distinct()
            .Where(File.Exists);
        
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

    private async Task<List<PackageUpdateResult>> TryUpdatePackages(RepositoryProjectTree repositoryProjectTree)
    {
        var results = new List<PackageUpdateResult>();
        
        var allProjectsInThisSolution = repositoryProjectTree.AllProjects
            .Where(x => File.Exists(x.AbsoluteFilePath))
            .Select(x => ProjectRootElement.Open(x.AbsoluteFilePath))
            .ToArray();
        
        var packageReferencesGrouped = allProjectsInThisSolution
            .SelectMany(p => p.Items.Where(i => i.ItemType == "PackageReference"))
            .GroupBy(x => x.Include);

        var nugetDependencyDetails = await _nuGetClient.GetPackages(packageReferencesGrouped.Select(x => x.Key));

        // if (TryUpdateAllPackagesSimultaneously(nugetDependencyDetails, out var updateAllResults))
        // {
        //     _logger.LogInformation("Successfully updated all projects simultaneously");
        //     return updateAllResults;
        // }
        
        _logger.LogInformation("Build errors - Falling back to updating packages one by one");
        
        foreach (var nuGetPackageInformation in nugetDependencyDetails
                     .Where(x => x?.PackageName != null)
                     .OrderBy(x => x.PackageName))
        {
            var tagsWithThisNugetPackage =
                packageReferencesGrouped.FirstOrDefault(x => x.Key == nuGetPackageInformation.PackageName);

            if (tagsWithThisNugetPackage == null)
            {
                continue;
            }

            var packageReferencesList = tagsWithThisNugetPackage.ToList();
            
            var projectsToBuild = packageReferencesList
                .SelectMany(x =>
                    repositoryProjectTree.GetUppermostProjectsReferencingThisProject(x.ContainingProject.Location.File))
                .Select(x => x.AbsoluteFilePath)
                .ToArray();

            await TryUpdatePackages(projectsToBuild, packageReferencesList,
                nuGetPackageInformation.Version.ToNormalizedString(),
                results, nuGetPackageInformation.PackageName);
        }

        // Some packages might not install initially because they depended on a newer version of another package first
        // So we can retry again, until all previously failed packages fail, so we know none are going to be successful again.
        int successCount;
        do
        {
            successCount = 0;
            foreach (var update in results.Where(x => x.PackageDowngradeDetected))
            {
                var packageReferencesList = packageReferencesGrouped.First(x => x.Key == update.PackageName).ToList();

                var projectsToBuild = packageReferencesList
                    .SelectMany(x =>
                        repositoryProjectTree.GetUppermostProjectsReferencingThisProject(x.ContainingProject.Location.File))
                    .Select(x => x.AbsoluteFilePath)
                    .ToArray();

                if (await TryUpdatePackages(projectsToBuild, packageReferencesList, update.NewPackageVersion, results,
                        update.PackageName))
                {
                    successCount++;
                }
            }
        } while (successCount > 0);
        
        return results;
    }
    
    private async Task<List<PackageUpdateResult>> TryDeleteUnusedPackages(RepositoryProjectTree repositoryProjectTree)
    {
        var results = new List<PackageUpdateResult>();

        var allProjectsInThisSolution = repositoryProjectTree.AllProjects
            .Where(x => File.Exists(x.AbsoluteFilePath))
                .Select(x => ProjectRootElement.Open(x.AbsoluteFilePath))
                .ToArray();

        foreach (var packageReference in allProjectsInThisSolution
                     .SelectMany(p => p.Items.Where(i => i.ItemType == "PackageReference"))
                     .Where(packageReferenceItem => !_packagesToNotRemoveRules.Any(rule => rule(packageReferenceItem)))
                 )
        {
            var name = packageReference.Include;
            var projectPath = packageReference.ContainingProject.Location.File;
            var version = packageReference.Metadata.First(m => m.Name == "Version").Value;

            if (!_previousResultsService.ShouldTryRemove(name, projectPath))
            {
                _logger.LogDebug("Skipping removal of {Package} from {Project} due to it not being successful in a previous run", name, packageReference.ContainingProject.Location.File);
                continue;
            }
            
            var parent = packageReference.Parent;
            var lastSibling = packageReference.PreviousSibling;
            var nextSibling = packageReference.NextSibling;

            packageReference.Parent.RemoveChild(packageReference);
            packageReference.ContainingProject.Save();

            if (await _packageVersionScanner.DowngradeDetected(repositoryProjectTree, projectPath, name, version))
            {
                UndoPackageRemoval(packageReference, parent, name, lastSibling, nextSibling);
                continue;
            }

            var projectsToBuild = repositoryProjectTree.GetUppermostProjectsReferencingThisProject(projectPath)
                .Select(x => x.AbsoluteFilePath)
                .ToList();

            var build = await _solutionBuilder.BuildProjects(projectsToBuild);
            if (build.IsSuccessful)
            {
                if (!parent.Children.Any())
                {
                    parent.Parent.RemoveChild(parent);
                    parent.ContainingProject.Save();
                }

                _logger.LogInformation("Package {PackageName} was successfully removed from Project {ProjectPath}",
                    name, projectPath);
                
                results.Add(new PackageUpdateResult
                {
                    UpdateBuiltSuccessfully = true,
                    PackageName = name,
                    NewPackageVersion = "Removed",
                });
            }
            else
            {
                UndoPackageRemoval(packageReference, parent, name, lastSibling, nextSibling);
            }
        }

        return results;
    }

    private void UndoPackageRemoval(ProjectItemElement packageReference, ProjectElementContainer parent, string name,
        ProjectElement lastSibling, ProjectElement nextSibling)
    {
        _logger.LogDebug("Package {PackageName} could not be removed from Project {ProjectPath}",
            packageReference.Include, parent.ContainingProject.Location.File);

        _previousResultsService.WriteUnableToRemovePackageEntry(name, packageReference.ContainingProject.Location.File);

        if (lastSibling != null)
        {
            parent.InsertAfterChild(packageReference, lastSibling);
        }
        else if (nextSibling != null)
        {
            parent.InsertBeforeChild(packageReference, nextSibling);
        }
        else
        {
            parent.AppendChild(packageReference);
        }

        packageReference.ContainingProject.Save();
    }

    // private async Task TryUpdateNetVersion(string[] solutionPaths)
    // {
    //     var targetFrameworkElements = solutionPaths
    //         .Select(SolutionFile.Parse)
    //         .SelectMany(s => s.ProjectsInOrder)
    //         .Where(p => File.Exists(p.AbsolutePath))
    //         .Select(p => ProjectRootElement.Open(p.AbsolutePath))
    //         .SelectMany(x => x.Properties)
    //         .Where(x => x.Name is "TargetFramework")
    //         .Where(x => !x.Value.Contains("netstandard"))
    //         // Net Framework has no dots e.g. net472 vs net6.0
    //         .Where(x => x.Value.Contains('.'))
    //         .Where(x => x.Value != LatestNetValue)
    //         .Select(x => new TargetFrameworkElementXmlWrapper
    //         {
    //             OldVersion = x.Value,
    //             XmlElement = x
    //         })
    //         .ToList();
    //
    //     foreach (var targetFrameworkElement in targetFrameworkElements)
    //     {
    //         targetFrameworkElement.XmlElement.Value = LatestNetValue;
    //     }
    //
    //     var projects = targetFrameworkElements
    //         .Select(x => x.XmlElement.ContainingProject.Location.File)
    //         .Distinct()
    //         .ToList();
    //     
    //     var parentProjects = _repositoryTreeGenerator.Generate()
    //     
    //     var projectsToBuild 
    //     
    //     SaveProjects(projects);
    //
    //     var solutionBuildResult = await _solutionBuilder.BuildProjects(solutionPaths, new List<ProjectItemElement>());
    //
    //     var solutionBuiltSuccessfully = solutionBuildResult.IsSuccessful;
    //         
    //     if (!solutionBuiltSuccessfully)
    //     {
    //         foreach (var targetFrameworkElement in targetFrameworkElements)
    //         {
    //             targetFrameworkElement.XmlElement.Value = targetFrameworkElement.OldVersion;
    //         }
    //
    //         SaveProjects(projects);
    //             
    //         _logger.LogWarning(".NET Version Update from {OldVersion} to {LatestVersion} failed", targetFrameworkElements.First().OldVersion, LatestNetValue);
    //     }
    //     else
    //     {
    //         _logger.LogDebug(".NET Version Update from {OldVersion} to {LatestVersion} was successful", targetFrameworkElements.First().OldVersion, LatestNetValue);
    //     }
    // }

    private async Task<bool> TryUpdatePackages(IReadOnlyCollection<string> projectsToBuild,
        IReadOnlyCollection<ProjectItemElement> packageReferences, string latestVersion,
        List<PackageUpdateResult> packageUpdateResults, string packageName)
        {
            if (!packageReferences.Any())
            {
                _logger.LogDebug("No PackageReference Tags found for package {Package}", packageName);
            }
            
            if (packageReferences.Any(x => x.Parent.Condition.Contains("TargetFramework"))
                || packageReferences.Any(x => x.Condition.Contains("TargetFramework")))
            {
                // Need to think about logic for this.
                // Different frameworks need different versions.
                return false;
            }
            
            var projects = packageReferences.Select(x => x.ContainingProject)
                .ToList();

            var versions = packageReferences
                .Select(p => p.Metadata.First(m => m.Name == "Version"))
                .Where(v => VersionsNeedsUpdating(latestVersion, v.Value))
                .Select(x => new PackageElementXmlWrapper
                {
                    XmlElement = x,
                    OldVersion = x.Value
                })
                .ToList();

            if (!versions.Any())
            {
                _logger.LogDebug("No out of date versions found for package {Package}", packageName);
                return false;
            }

            foreach (var version in versions)
            {
                version.XmlElement.Value = latestVersion;
            }
        
            await SaveProjects(projects);

            var solutionBuildResult = await _solutionBuilder.BuildProjects(projectsToBuild);

            var solutionBuiltSuccessfully = solutionBuildResult.IsSuccessful;
            
            if (!solutionBuiltSuccessfully)
            {
                foreach (var version in versions)
                {
                    version.XmlElement.Value = version.OldVersion;
                }

                await SaveProjects(projects);

                _logger.LogWarning("Package {PackageName} upgrade from {OldVersion} to {LatestVersion} failed", packageName, versions.First().OldVersion, latestVersion);
            }
            else
            {
                _logger.LogInformation("Package {PackageName} upgrade from {OldVersion} to {LatestVersion} was successful", packageName, versions.First().OldVersion, latestVersion);
            }
        
            UpdateInfo(latestVersion, packageUpdateResults, packageName, versions, solutionBuildResult);

            return solutionBuiltSuccessfully;
        }

    private static bool VersionsNeedsUpdating(string latestVersion, string originalVersion)
    {
        if (originalVersion == latestVersion)
        {
            return false;
        }

        if (SemVersion.TryParse(originalVersion, SemVersionStyles.Any, out var tagVersion) 
            && SemVersion.TryParse(latestVersion, SemVersionStyles.Any, out var latestNugetVersion))
        {
            
            return tagVersion < latestNugetVersion;
        }

        return true;
    }

    private static void UpdateInfo(string latestVersion, ICollection<PackageUpdateResult> packageUpdateResults, string packageName, List<PackageElementXmlWrapper> packageElementXmlWrappers,
            SolutionBuildResult solutionBuildResult)
        {
            var existingElement = packageUpdateResults.FirstOrDefault(x => x.PackageName == packageName);

            var oldVersion = packageElementXmlWrappers.First().OldVersion;
            
            if (existingElement != null)
            {
                existingElement.NewPackageVersion = latestVersion;
                existingElement.OldPackageVersion = oldVersion;
                existingElement.UpdateBuiltSuccessfully = solutionBuildResult.IsSuccessful;
                existingElement.PackageDowngradeDetected = solutionBuildResult.DetectedDowngrade;
                existingElement.FileLines = solutionBuildResult.IsSuccessful
                    ? new List<string>()
                    : GetFileLineLocations(packageElementXmlWrappers);

                return;
            }
        
            packageUpdateResults.Add(new PackageUpdateResult
            {
                PackageName = packageName,
                NewPackageVersion = latestVersion,
                OldPackageVersion = oldVersion,
                UpdateBuiltSuccessfully = solutionBuildResult.IsSuccessful,
                PackageDowngradeDetected = solutionBuildResult.DetectedDowngrade,
                FileLines = solutionBuildResult.IsSuccessful ? new List<string>() : GetFileLineLocations(packageElementXmlWrappers)
            });
        }

        private static List<string> GetFileLineLocations(List<PackageElementXmlWrapper> packageElementXmlWrappers)
        {
            return packageElementXmlWrappers.Select(x => Path.GetFileName(x.XmlElement.Parent.Location.File) + $" | Line: {x.XmlElement.Parent.Location.Line}").ToList();
        }

        private static Task SaveProjects(IEnumerable<ProjectRootElement> allProjects)
        {
            foreach (var project in allProjects)
            {
                project.Save();
            }
            
            return Task.CompletedTask;
        }
}