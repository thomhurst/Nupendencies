using System.Runtime.InteropServices;
using NuGet.ProjectModel;
using Semver;

namespace TomLonghurst.Nupendencies;

public interface IPackageVersionScanner
{
    Task<DependencyGraphSpec> GenerateDependencyGraph(string projectPath);

    Task<bool> DowngradeDetected(Project project, string packageName, SemVersion version);
}