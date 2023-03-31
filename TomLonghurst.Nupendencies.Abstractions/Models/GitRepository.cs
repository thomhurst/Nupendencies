using LibGit2Sharp;
using TomLonghurst.Nupendencies.Abstractions.Contracts;

namespace TomLonghurst.Nupendencies.Abstractions.Models;

public record GitRepository
{
    public required IGitProvider Provider { get; set; }
    public required Credentials Credentials { get; set; }
    public required string Owner { get; set; }

    public required string Name { get; set; }

    public required string Id { get; set; }

    public required bool IsDisabled { get; set; }

    public required List<GitIssue> Issues { get; set; }

    public required string GitUrl { get; set; }

    public required string MainBranch { get; set; }
}