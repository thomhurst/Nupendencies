namespace TomLonghurst.Nupendencies;

public class ProjectBuildResult
{
    public int ExitCode { get; set; }
    public string Output { get; set; }
    public string ErrorOutput { get; set; }
}