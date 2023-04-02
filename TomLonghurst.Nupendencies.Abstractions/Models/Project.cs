using System.Collections.Immutable;
using Microsoft.Build.Construction;
using Semver;
using TomLonghurst.Nupendencies.Abstractions.Extensions;

namespace TomLonghurst.Nupendencies.Abstractions.Models;

public record Project
{
    public string Name { get; }
    public bool IsNetCore { get; }
    public CodeRepository Repository { get; }
    public string ProjectPath { get; }
    public string Directory { get; }
    public HashSet<Project> DirectoryBuildProps { get; } = new();
    public HashSet<Project> Parents { get; } = new();
    public ImmutableHashSet<ChildProject> Children { get; }
    public ImmutableHashSet<ProjectPackage> Packages { get; }
    public HashSet<Solution> Solutions { get; } = new();
    public ProjectRootElement ProjectRootElement { get; }

    public bool IsMultiTargeted { get; }

    public Project(CodeRepository repository, Solution solution, string filePath) : this(repository, new []{ solution }, filePath)
    {
    }

    public Project(CodeRepository repository, IEnumerable<Solution> solutions, string filePath)
    {
        ProjectPath = filePath;

        Repository = repository;
        
        Name = Path.GetFileNameWithoutExtension(filePath);
        
        Directory = Path.GetDirectoryName(filePath)!;

        Solutions.AddRange(solutions);

        foreach (var s in Solutions)
        {
            s.Projects.Add(this);
        }
        
        DirectoryBuildProps = GetDirectoryBuildProps(Directory).ToHashSet();

        ProjectRootElement = ProjectRootElement.Open(filePath)!;

        TargetFrameworks = new TargetFrameworks(this);
        TargetFramework = new TargetFramework(this);
        
        IsMultiTargeted = TargetFrameworks.IsMultiTargeted || DirectoryBuildProps.Any(x => x.IsMultiTargeted);

        IsNetCore = TargetFramework.IsNetCore
                    || TargetFrameworks.IsNetCore
                    || DirectoryBuildProps.Any(dbp => dbp.TargetFramework.IsNetCore || dbp.TargetFrameworks.IsNetCore);
            
        Packages = ParsePackages(ProjectRootElement);
        Children = ParseChildren(ProjectRootElement);
    }

    public TargetFramework TargetFramework { get; init; }

    public TargetFrameworks TargetFrameworks { get; init; }

    private string? GetPropertyValue(string propertyName)
    {
        return ProjectRootElement.Properties
            .FirstOrDefault(x => x.Name == propertyName)
            ?.Value;
    }

    private IEnumerable<Project> GetDirectoryBuildProps(string? directory)
    {
        if (ProjectPath.Contains("Directory.Build.props", StringComparison.InvariantCultureIgnoreCase))
        {
            IsBuildable = false;
            yield break;
        }
        
        while (directory != null)
        {
            foreach (var directoryBuildProp in System.IO.Directory.GetFiles(directory, "Directory.Build.props", SearchOption.TopDirectoryOnly)
                         .Select(x => new Project(Repository, Solutions, x)))
            {
                yield return directoryBuildProp;
            }

            directory = System.IO.Directory.GetParent(directory)?.FullName;
        }
    }

    public IEnumerable<Project> GetUppermostProjectsReferencingThisProject()
    {
        return GetUppermostProjects().DistinctBy(x => x.ProjectPath);
    }

    public ProjectPackage? GetPackage(string packageName)
    {
        return Packages.FirstOrDefault(p => string.Equals(p.Name, packageName, StringComparison.OrdinalIgnoreCase));
    }

    public void Save() => ProjectRootElement.Save();

    private IEnumerable<Project> GetUppermostProjects()
    {
        if (!Parents.Any())
        {
            yield return this;
            yield break;
        }

        foreach (var project in Parents.SelectMany(projectParent =>
                     projectParent.GetUppermostProjectsReferencingThisProject()))
        {
            yield return project;
        }
    }

    private ImmutableHashSet<ProjectPackage> ParsePackages(ProjectRootElement projectRootElement)
    {
        var packages = projectRootElement.Items
            .Where(i => i.ItemType == "PackageReference")
            .Select(packageReferenceTag =>
            {
                var versionElement = packageReferenceTag.Metadata.FirstOrDefault(m => m.Name == "Version");
                
                var version = versionElement?.Value;

                if (string.IsNullOrEmpty(version) || !SemVersion.TryParse(version, SemVersionStyles.Any, out var semVersion))
                {
                    return null;
                }
                
                return new ProjectPackage(packageReferenceTag)
                {
                    Name = packageReferenceTag.Include,
                    Project = this,
                    OriginalVersion = semVersion,
                    CurrentVersion = semVersion
                };
            })
            .OfType<ProjectPackage>()
            .ToList();

        DeleteDuplicatePackages();
        
        return packages.ToImmutableHashSet();

        void DeleteDuplicatePackages()
        {
            foreach (var packageGroup in packages
                         .Where(p => !p.IsConditional)
                         .GroupBy(x => x.Name)
                         .Where(x => x.Count() > 1))
            {
                var packagesToRemove = packageGroup.OrderByDescending(x => x.OriginalVersion).Skip(1);
                foreach (var packageToRemove in packagesToRemove)
                {
                    packageToRemove.Remove();
                    packages.Remove(packageToRemove);
                }
            }
        }
    }

    private ImmutableHashSet<ChildProject> ParseChildren(ProjectRootElement projectRootElement)
    {
        var children = projectRootElement.Items
            .Where(i => i.ItemType == "ProjectReference")
            .Where(projectReference => File.Exists(GetFullPath(projectReference.Include)))
            .Where(projectReference => GetFullPath(projectReference.Include) != ProjectPath)
            .DistinctBy(projectReference => GetFullPath(projectReference.Include))
            .Select(projectReference =>
            {
                var path = GetFullPath(projectReference.Include);

                var project = Repository.GetProject(path) ?? new Project(Repository, Solutions, path);
                
                return new ChildProject
                {
                    ParentProject = this,
                    Project = project,
                    ProjectReferenceTag = projectReference
                };
            })
            .ToList();

        foreach (var child in children)
        {
            child.Project.Solutions.AddRange(Solutions);
            child.Project.Solutions.ForEach(s => s.Projects.Add(child.Project));
            child.Project.Parents.Add(this);
        }

        return children.ToImmutableHashSet();
    }

    private string GetFullPath(string path)
    {
        return Path.IsPathFullyQualified(path) ? path : Path.GetFullPath(path, Directory);
    }

    private sealed class FilePathEqualityComparer : IEqualityComparer<Project>
    {
        public bool Equals(Project? x, Project? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.ProjectPath == y.ProjectPath;
        }

        public int GetHashCode(Project obj)
        {
            return obj.ProjectPath.GetHashCode();
        }
    }

    public static IEqualityComparer<Project> Comparer { get; } = new FilePathEqualityComparer();
    public bool IsBuildable { get; private set; } = true;

    public virtual bool Equals(Project? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return ProjectPath == other.ProjectPath;
    }

    public override int GetHashCode()
    {
        return ProjectPath.GetHashCode();
    }
}