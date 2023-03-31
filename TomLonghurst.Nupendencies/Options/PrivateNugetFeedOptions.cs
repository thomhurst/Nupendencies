namespace TomLonghurst.Nupendencies.Options;

public class PrivateNugetFeedOptions
{
    public required string Username { get; init; }
    public required string PatToken { get; init; }
    public required string SourceName { get; init; }
    public required string SourceUrl { get; init; }
}