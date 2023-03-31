namespace TomLonghurst.Nupendencies;

public class ProjectBuildResult
{
    public int ExitCode { get; init; }
    public string Output { get; init; }
    public string ErrorOutput { get; init; }
}