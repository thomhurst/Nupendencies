namespace TomLonghurst.Nupendencies.Abstractions.Models;

public record GitPullRequest
{
    public required int Number { get; set; }
    public required string Id { get; set; }
    public required string Title { get; set; }
    public required string Body { get; set; }
    public required bool HasConflicts { get; set; }
}