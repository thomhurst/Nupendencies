namespace TomLonghurst.Nupendencies.Options;

public class PrivateNugetFeedOptions
{
    public required string? Username { get; set; }
    public required string? PatToken { get; set; }
    public required string SourceName { get; set; }
    public required string SourceUrl { get; set; }
}