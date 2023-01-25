namespace TomLonghurst.Nupendencies.Services;

public class SolutionBuildResult
{
    public bool IsSuccessful { get; set; }
    public List<string> OutputErrors { get; set; } = new();
    public string OutputString => string.Join(Environment.NewLine, OutputErrors);
    public bool DetectedDowngrade => OutputErrors.Any(x => x.Contains("Detected package downgrade"));
}