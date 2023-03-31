using LibGit2Sharp;
using TomLonghurst.Nupendencies.Abstractions.Contracts;

namespace TomLonghurst.Nupendencies.Abstractions.Models;

public record GitRepository
{
    public required IGitProvider Provider { get; init; }
    public required Credentials Credentials { get; init; }
    public required string Owner { get; init; }

    public required string Name { get; init; }

    public required string Id { get; init; }

    public required bool IsDisabled { get; init; }

    public required List<GitIssue> Issues { get; init; }

    public required string GitUrl { get; init; }

    public required string MainBranch { get; init; }
}