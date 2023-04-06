using TomLonghurst.Nupendencies.Abstractions.Models;

namespace TomLonghurst.Nupendencies.Options;

public record NupendenciesOptions
{
    public bool TryRemoveUnusedPackages { get; init; }
    public bool TryRemoveUnusedProjects { get; set; }

    public List<PrivateNugetFeedOptions> PrivateNugetFeedOptions { get; } = new();
    public Func<GitRepository, bool>? RepositoriesToScan { get; set; }
    public Func<ProjectPackage, bool>? PackagesToUpdate { get; set; }

    public required string CommitterName { get; init; }
    public required string CommitterEmail { get; init; }
}