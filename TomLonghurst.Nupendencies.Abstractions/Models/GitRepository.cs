namespace TomLonghurst.Nupendencies.Abstractions.Models;

public record GitRepository
{
    public required string Owner { get; set; }

    public required string Name { get; set; }

    public required string Id { get; set; }

    public required bool IsDisabled { get; set; }

    public required List<GitIssue> Issues { get; set; }

    public required string GitUrl { get; set; }

    public required string MainBranch { get; set; }
}