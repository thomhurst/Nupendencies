namespace TomLonghurst.Nupendencies.Abstractions.Models;

public record GitIssue
{
    public required int IssueNumber { get; set; }
    public required string Id { get; set; }
    public required string Title { get; set; }
    public required string Author { get; set; }
    public required DateTimeOffset Created { get; set; }
    public required DateTimeOffset LastUpdated { get; set; }
    public required bool IsClosed { get; set; }
}