namespace TomLonghurst.Nupendencies.Abstractions.Models;

public record GitIssue
{
    public required int IssueNumber { get; init; }
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Author { get; init; }
    public required DateTimeOffset Created { get; init; }
    public required DateTimeOffset LastUpdated { get; init; }
    public required bool IsClosed { get; init; }
}