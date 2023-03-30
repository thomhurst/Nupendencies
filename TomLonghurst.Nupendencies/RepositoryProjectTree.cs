using System.Collections.Immutable;
using Microsoft.Build.Construction;
using NuGet.LibraryModel;
using NuGet.Packaging;

namespace TomLonghurst.Nupendencies;

public record RepositoryProjectTree
{
    private readonly IReadOnlyList<ProjectTreeItem> _projectTreeItems;
    private readonly IReadOnlyList<LibraryDependency> _originalDependencies;

    public ImmutableList<ProjectTreeItem> AllProjects => _projectTreeItems.ToImmutableList();
    public ImmutableList<LibraryDependency> OriginalDependencies => _originalDependencies.ToImmutableList();

    public RepositoryProjectTree(IEnumerable<ProjectTreeItem> projectTreeItems,
        IReadOnlyList<LibraryDependency> dependencies)
    {
        _projectTreeItems = projectTreeItems.Distinct().ToList();
        _originalDependencies = dependencies;
    }
    
    public IEnumerable<ProjectTreeItem> GetUppermostProjectsReferencingThisProject(ProjectRootElement projectRootElement)
    {
        return GetUppermostProjectsReferencingThisProject(projectRootElement.Location.File);
    }

    public IEnumerable<ProjectTreeItem> GetUppermostProjectsReferencingThisProject(ProjectTreeItem projectTreeItem)
    {
        return GetUppermostProjectsReferencingThisProject(projectTreeItem.AbsoluteFilePath);
    }

    public IEnumerable<ProjectTreeItem> GetUppermostProjectsReferencingThisProject(string projectFilePath)
    {
        return GetUppermostProjectsReferencingThisProject(projectFilePath, new HashSet<ProjectTreeItem>(), new HashSet<string>());
    }

    private IEnumerable<ProjectTreeItem> GetUppermostProjectsReferencingThisProject(string projectFilePath, HashSet<ProjectTreeItem> collection, HashSet<string> filesProcessed)
    {
        if (filesProcessed.Any(currentFilePath => currentFilePath == projectFilePath))
        {
            return new List<ProjectTreeItem>();
        }
        
        var project = _projectTreeItems.FirstOrDefault(p => p.AbsoluteFilePath == projectFilePath);

        filesProcessed.Add(projectFilePath);
        
        if (project == null)
        {
            return new List<ProjectTreeItem>();
        }

        if (!project.Parents.Any())
        {
            return new List<ProjectTreeItem>
            {
                project
            };
        }

        foreach (var parent in project.Parents)
        {
            collection.AddRange(GetUppermostProjectsReferencingThisProject(parent.AbsoluteFilePath, collection, filesProcessed));
        }

        return collection;
    }
}