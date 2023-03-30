using System.Collections.Immutable;

namespace TomLonghurst.Nupendencies.Services;

public interface ISolutionBuilder
{
    Task<SolutionBuildResult> BuildProjects(ImmutableHashSet<Project> projects, string target = "build");
    // Task<SolutionBuildResult> BuildSolutions(IReadOnlyCollection<string> solutionsToBuild, IReadOnlyCollection<ProjectItemElement> packageReferenceElements);
}