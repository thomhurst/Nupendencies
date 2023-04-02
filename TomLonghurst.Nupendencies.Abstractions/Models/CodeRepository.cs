using System.Collections.Immutable;
using System.Text;
using TomLonghurst.Nupendencies.Abstractions.Extensions;

namespace TomLonghurst.Nupendencies.Abstractions.Models;

public record CodeRepository
{
    public string Name { get; }
    public string RepositoryPath { get; }
    public HashSet<Solution> Solutions { get; } = new();
    public HashSet<Project> AllProjects { get; } = new();


    public CodeRepository(string repositoryDirectoryPath, GitRepository gitRepository)
    {
        Name = gitRepository.Name;

        RepositoryPath = repositoryDirectoryPath;

        var solutionFilePaths = Directory.GetFiles(repositoryDirectoryPath, "*.sln", SearchOption.AllDirectories);

        Solutions.AddRange(solutionFilePaths
            .Select(path => new Solution(this, path))
            .ToImmutableHashSet()
        );

        AllProjects.AddRange(Solutions
            .SelectMany(s => s.Projects)
            .ToImmutableHashSet()
        );
    }

    public Project? GetProject(string filePath)
    {
        return Solutions?.SelectMany(s => s.Projects)
            .FirstOrDefault(p => p.ProjectPath == filePath);
    }

    private sealed class NameEqualityComparer : IEqualityComparer<CodeRepository>
    {
        public bool Equals(CodeRepository? x, CodeRepository? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Name == y.Name;
        }

        public int GetHashCode(CodeRepository obj)
        {
            return obj.Name.GetHashCode();
        }
    }

    public static IEqualityComparer<CodeRepository> Comparer { get; } = new NameEqualityComparer();

    public virtual bool Equals(CodeRepository? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Name == other.Name;
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }

    protected virtual bool PrintMembers(StringBuilder builder)
    {
        builder.AppendLine($"Repository: {Name}");
        foreach (var solution in Solutions)
        {
            builder.AppendLine("\t- Solution:" + solution.Name);
            
            foreach (var project in solution.Projects.GetProjectsToBuild())
            {
                PrintProject(2, builder, project);
            }
        }

        return true;
    }

    private void PrintProject(int indentLevel, StringBuilder builder, Project project)
    {
        builder.AppendLine($"{new string('\t', indentLevel)}- Project: " + project.Name);
        
        project.Children.ForEach(child => PrintProject(indentLevel+1, builder, child.Project));
    }
}