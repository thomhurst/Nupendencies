using TomLonghurst.Nupendencies.Abstractions.Models;

namespace TomLonghurst.Nupendencies.Options;

public class NupendenciesOptions
{
    public required bool TryRemoveUnusedPackages { get; set; }

    public List<PrivateNugetFeedOptions> PrivateNugetFeedOptions { get; } = new();
    public List<Func<GitRepository, bool>> RepositoriesToScan { get; set; } = new();
    
    public required string CommitterName { get; set; }
    public required string CommitterEmail { get; set; }
}