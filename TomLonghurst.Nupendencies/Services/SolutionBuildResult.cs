using System.Collections.Immutable;
using TomLonghurst.Nupendencies.Abstractions.Models;

namespace TomLonghurst.Nupendencies.Services;

public class SolutionBuildResult
{
    public required bool IsSuccessful { get; init; }
    public required List<string> OutputErrors { get; init; } = new();
    public string OutputString => string.Join(Environment.NewLine, OutputErrors);
    public bool DetectedDowngrade => OutputErrors.Any(x => x.Contains("Detected package downgrade"));
    public required ImmutableHashSet<Project> BuiltProjects { get; init; }
}