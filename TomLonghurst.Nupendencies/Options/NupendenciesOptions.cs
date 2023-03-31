using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Options;

public class NupendenciesOptions
{
    public GithubOptions GithubOptions { get; set; } = new();
    public AzureDevOpsOptions AzureDevOpsOptions { get; set; } = new();
    
    public bool TryRemoveUnusedPackages { get; set; }

    public List<PrivateNugetFeedOptions> PrivateNugetFeedOptions { get; } = new();
    public List<Func<GitRepository, bool>> RepositoriesToScan { get; set; } = new();
    
    public string CommitterName { get; set; }
    public string CommitterEmail { get; set; }
}