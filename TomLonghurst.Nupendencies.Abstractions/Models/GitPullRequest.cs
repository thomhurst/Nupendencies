namespace TomLonghurst.Nupendencies.Abstractions.Models;

public record GitPullRequest
{
    public required int Number { get; init; }
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Body { get; init; }
    public required bool HasConflicts { get; init; }
}