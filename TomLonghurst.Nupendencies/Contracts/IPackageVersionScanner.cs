using NuGet.ProjectModel;
using Semver;
using TomLonghurst.Nupendencies.Abstractions.Models;

namespace TomLonghurst.Nupendencies.Contracts;

public interface IPackageVersionScanner
{
    Task<DependencyGraphSpec?> GenerateDependencyGraph(string projectPath);

    Task<bool> DowngradeDetected(Project project, string packageName, SemVersion version);
}