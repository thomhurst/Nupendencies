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
    public ImmutableHashSet<Project> Children { get; }
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

        TargetFrameworks = new TargetFrameworks(ProjectRootElement.Properties.FirstOrDefault(x => x.Name == "TargetFrameworks"));
        TargetFramework = new TargetFramework(this, ProjectRootElement.Properties.FirstOrDefault(x => x.Name == "TargetFrameworks"));
        
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
        return Enumerable.DistinctBy(GetUppermostProjects(), x => x.ProjectPath);
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

    private ImmutableHashSet<Project> ParseChildren(ProjectRootElement projectRootElement)
    {
        var children = projectRootElement.Items
            .Where(i => i.ItemType == "ProjectReference")
            .Select(x => x.Include)
            .Select(GetFullPath)
            .Where(File.Exists)
            .Where(path => path != ProjectPath)
            .Distinct()
            .Select(path => Repository.GetProject(path) ?? new Project(Repository, Solutions, path))
            .ToList();

        foreach (var child in children)
        {
            child.Solutions.AddRange(Solutions);
            child.Solutions.ForEach(s => s.Projects.Add(child));
            child.Parents.Add(this);
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

public record TargetFramework
{
    private string? _currentValue;
    public Project Project { get; }
    public ProjectPropertyElement? TargetFrameworkTag { get; init; }

    public TargetFramework(Project project, ProjectPropertyElement? targetFrameworkTag)
    {
        Project = project;
        TargetFrameworkTag = targetFrameworkTag;
        OriginalValue = targetFrameworkTag?.Value;
        CurrentValue = OriginalValue;
    }

    public string? OriginalValue { get; }

    public string? CurrentValue
    {
        get => _currentValue;
        init
        {
            if (OriginalValue != null)
            {
                TargetFrameworkTag!.Value = value;
                Project.Save();
                _currentValue = value;
            }
        }
    }

    public void Rollback()
    {
        CurrentValue = OriginalValue;
    }
    
    public bool HasValue => !string.IsNullOrWhiteSpace(OriginalValue);
    
    public bool IsNetCore => NetCoreParser.IsNetCore(OriginalValue);
}

public record TargetFrameworks(ProjectPropertyElement? TargetFrameworksTag)
{
    public string[] Values => TargetFrameworksTag?.Value?
        .Split(';')
        .Select(x => x.Trim())
        .ToArray() ?? Array.Empty<string>();

    public bool HasValues => Values.Any();

    public bool IsMultiTargeted => Values.Length > 1;

    public bool IsNetCore => Values.Any(NetCoreParser.IsNetCore);
}

public class NetCoreParser
{
    public static bool IsNetCore(string? frameworkVersion)
    {
        if (string.IsNullOrWhiteSpace(frameworkVersion))
        {
            return false;
        }

        if (frameworkVersion.StartsWith("netcoreapp", StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }
        
        if (frameworkVersion.StartsWith("net", StringComparison.InvariantCultureIgnoreCase)
            && frameworkVersion.Contains('.'))
        {
            return true;
        }

        return false;
    }
}