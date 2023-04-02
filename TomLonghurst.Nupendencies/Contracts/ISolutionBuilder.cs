using System.Collections.Immutable;
using TomLonghurst.Nupendencies.Abstractions.Models;
using TomLonghurst.Nupendencies.Services;

namespace TomLonghurst.Nupendencies.Contracts;

public interface ISolutionBuilder
{
    Task<SolutionBuildResult> BuildProjects(ImmutableHashSet<Project> projects, string target = "build");
}