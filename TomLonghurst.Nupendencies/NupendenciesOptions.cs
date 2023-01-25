using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies;

public class NupendenciesOptions
{
    public GithubOptions GithubOptions { get; set; } = new();
    public AzureDevOpsOptions AzureDevOpsOptions { get; set; } = new();

    public List<PrivateNugetFeedOptions> PrivateNugetFeedOptions { get; } = new();
    public List<Func<Repo, bool>> RepositoriesToScan { get; set; } = new();
    
    public string CommitterName { get; set; }
    public string CommitterEmail { get; set; }
}