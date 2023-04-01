namespace TomLonghurst.Nupendencies.Models;

public class ProjectBuildResult
{
    public required int ExitCode { get; init; }
    public required string Output { get; init; }
}