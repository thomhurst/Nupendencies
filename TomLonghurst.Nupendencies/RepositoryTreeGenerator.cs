using Microsoft.Build.Construction;
using Microsoft.Extensions.Logging;
using NuGet.LibraryModel;
using NuGet.Packaging;
using TomLonghurst.EnumerableAsyncProcessor.Extensions;

namespace TomLonghurst.Nupendencies;

public class RepositoryTreeGenerator : IRepositoryTreeGenerator
{
    private readonly ILogger _logger;
    private readonly IPackageVersionScanner _packageVersionScanner;

    public RepositoryTreeGenerator(ILogger<RepositoryTreeGenerator> logger,
        IPackageVersionScanner packageVersionScanner)
    {
        _logger = logger;
        _packageVersionScanner = packageVersionScanner;
    }
    
    public async Task<RepositoryProjectTree> Generate(ProjectRootElement[] projects)
    {
        var projectTreeItems = new HashSet<ProjectTreeItem>();

        foreach (var projectRootElement in projects)
        {
            var currentFile = projectRootElement.Location.File;

            var projectTreeItem = projectTreeItems.FirstOrDefault(x => x.AbsoluteFilePath == currentFile)
                                  ?? new ProjectTreeItem(currentFile);
            
            projectTreeItems.Add(projectTreeItem);

            var currentDirectory = Path.GetDirectoryName(currentFile);

            var childProjectPaths = projectRootElement.Items
                .Where(i => i.ItemType == "ProjectReference")
                .Select(x => x.Include)
                .Select(x => Path.GetFullPath(x, currentDirectory))
                .Where(File.Exists)
                .Where(x => x != currentFile)
                .Distinct()
                .ToList();
            
            foreach (var childProjectPath in childProjectPaths)
            {
                var childProjectTreeItem = projectTreeItems.FirstOrDefault(x => x.AbsoluteFilePath == currentFile) 
                                           ?? new ProjectTreeItem(childProjectPath);

                if (!File.Exists(childProjectPath))
                {
                    _logger.LogWarning("File does not exist: {FilePath}", childProjectPath);
                    continue;
                }

                projectTreeItems.Add(childProjectTreeItem);
                
                projectTreeItem.AddChild(childProjectTreeItem);
                childProjectTreeItem.AddParent(projectTreeItem);
            }

            var parents = projects
                .SelectMany(x => x.Items)
                .Where(x => x != null)
                .Where(i => i.ItemType == "ProjectReference")
                .Where(p => Path.GetFullPath(p.Include, Path.GetDirectoryName(p.ContainingProject.Location.File)) == currentFile)
                .Select(x => projectTreeItems.FirstOrDefault(pti => pti.AbsoluteFilePath == x.ContainingProject.Location.File)
                             ?? new ProjectTreeItem(x.ContainingProject.Location.File))
                .Where(x => x.AbsoluteFilePath != currentFile)
                .Distinct()
                .ToList();
            
            foreach (var parent in parents)
            {
                if(!File.Exists(parent.AbsoluteFilePath))
                {
                    _logger.LogWarning("File does not exist: {FilePath}", parent.AbsoluteFilePath);
                    continue;
                }
                
                projectTreeItems.Add(parent);
                
                parent.AddChild(projectTreeItem);
                projectTreeItem.AddParent(parent);
            }
        }

        var dependencies = await GenerateDependencies(projectTreeItems);

        return new RepositoryProjectTree(projectTreeItems, dependencies);
    }

    private async Task<IReadOnlyList<LibraryDependency>> GenerateDependencies(HashSet<ProjectTreeItem> projectTreeItems)
    {
        var dependencyGraphs = await projectTreeItems
            .Where(x => File.Exists(x.AbsoluteFilePath))
            .ToAsyncProcessorBuilder()
            .SelectAsync(x => _packageVersionScanner.GenerateDependencyGraph(x.AbsoluteFilePath))
            .ProcessInParallel();

        return dependencyGraphs
            .Where(x => x != null)
            .SelectMany(x => x.Projects)
            .Where(x => x != null)
            .SelectMany(x => x.TargetFrameworks)
            .Where(x => x != null)
            .SelectMany(x => x.Dependencies)
            .Where(x => x != null)
            .ToList();
    }
}