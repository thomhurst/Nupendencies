namespace TomLonghurst.Nupendencies.Services;

public interface ISolutionBuilder
{
    Task<SolutionBuildResult> BuildProjects(IEnumerable<string> projects, string target = "build");
    // Task<SolutionBuildResult> BuildSolutions(IReadOnlyCollection<string> solutionsToBuild, IReadOnlyCollection<ProjectItemElement> packageReferenceElements);
}