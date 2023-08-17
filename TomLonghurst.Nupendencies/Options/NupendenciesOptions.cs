using TomLonghurst.Nupendencies.Abstractions.Models;
using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Options;

public record NupendenciesOptions
{
    public bool TryRemoveUnusedPackages { get; init; }
    public bool TryRemoveUnusedProjects { get; set; }
    
    public string? TryUpdateTargetFrameworkTo { get; set; }

    public bool RaiseIssuesForFailedUpdates { get; set; } = true;
    public bool RaisePullRequestsForSuccessfulUpdates { get; set; } = true;

    public List<PrivateNugetFeedOptions> PrivateNugetFeedOptions { get; } = new();
    public Func<GitRepository, bool>? ShouldUpdateRepositoryPredicate { get; set; }
    public Func<PackageUpdateModel, bool>? ShouldUpdatePackagePredicate { get; set; }

    public required string CommitterName { get; init; }
    public required string CommitterEmail { get; init; }
}