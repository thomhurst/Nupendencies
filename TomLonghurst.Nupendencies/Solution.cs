using Microsoft.Build.Construction;
using NuGet.Packaging;

namespace TomLonghurst.Nupendencies;

public record Solution
{
    public string Name { get; }
    public CodeRepository Repository { get; }
    public string SolutionPath { get; }
    public HashSet<Project> Projects { get; } = new();
    
    public Solution(CodeRepository repository, string solutionPath)
    {
        SolutionPath = solutionPath;
        
        Repository = repository;

        Name = Path.GetFileNameWithoutExtension(solutionPath);

        Projects.AddRange(SolutionFile.Parse(solutionPath)
            .ProjectsInOrder
            .Where(p => p.AbsolutePath.EndsWith(".csproj"))
            .Where(p => File.Exists(p.AbsolutePath))
            .DistinctBy(p => p.AbsolutePath)
            .Select(path => GetProject(path.AbsolutePath) ?? new Project(Repository, this, path.AbsolutePath))
        );
    }

    public Project? GetProject(string filePath)
    {
        return Repository.GetProject(filePath);
    }

    private sealed class SolutionPathEqualityComparer : IEqualityComparer<Solution>
    {
        public bool Equals(Solution? x, Solution? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.SolutionPath == y.SolutionPath;
        }

        public int GetHashCode(Solution obj)
        {
            return obj.SolutionPath.GetHashCode();
        }
    }

    public static IEqualityComparer<Solution> Comparer { get; } = new SolutionPathEqualityComparer();

    public virtual bool Equals(Solution? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return SolutionPath == other.SolutionPath;
    }

    public override int GetHashCode()
    {
        return SolutionPath.GetHashCode();
    }
}