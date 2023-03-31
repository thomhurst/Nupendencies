using TomLonghurst.Nupendencies.Abstractions.Models;

namespace TomLonghurst.Nupendencies.Options;

public class NupendenciesOptions
{
    public required bool TryRemoveUnusedPackages { get; init; }

    public List<PrivateNugetFeedOptions> PrivateNugetFeedOptions { get; } = new();
    public List<Func<GitRepository, bool>> RepositoriesToScan { get; init; } = new();
    
    public required string CommitterName { get; init; }
    public required string CommitterEmail { get; init; }
}