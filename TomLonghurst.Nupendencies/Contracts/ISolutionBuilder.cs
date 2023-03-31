using System.Collections.Immutable;
using TomLonghurst.Nupendencies.Services;

namespace TomLonghurst.Nupendencies.Contracts;

public interface ISolutionBuilder
{
    Task<SolutionBuildResult> BuildProjects(ImmutableHashSet<Project> projects, string target = "build");
    // Task<SolutionBuildResult> BuildSolutions(IReadOnlyCollection<string> solutionsToBuild, IReadOnlyCollection<ProjectItemElement> packageReferenceElements);
}