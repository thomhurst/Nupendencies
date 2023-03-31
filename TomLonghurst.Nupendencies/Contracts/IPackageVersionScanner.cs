using NuGet.ProjectModel;
using Semver;

namespace TomLonghurst.Nupendencies.Contracts;

public interface IPackageVersionScanner
{
    Task<DependencyGraphSpec?> GenerateDependencyGraph(string projectPath);

    Task<bool> DowngradeDetected(Project project, string packageName, SemVersion version);
}