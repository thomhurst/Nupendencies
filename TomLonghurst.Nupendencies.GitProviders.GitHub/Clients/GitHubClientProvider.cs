using System.Reflection;
using Octokit;
using Octokit.Internal;
using TomLonghurst.Nupendencies.Abstractions;
using TomLonghurst.Nupendencies.GitProviders.GitHub.Options;

namespace TomLonghurst.Nupendencies.GitProviders.GitHub.Clients;

internal class GitHubClientProvider : IGitHubClientProvider
{
    public GitHubClient GitHubClient { get; }

    public GitHubClientProvider(GitHubOptions gitHubOptions)
    {
        var version = Assembly.GetAssembly(typeof(GitHubClientProvider))?.GetName()?.Version?.ToString() ?? "1.0";
        
        GitHubClient = new GitHubClient(new ProductHeaderValue(NupendencyConstants.AppName, version), new InMemoryCredentialStore(new Credentials(gitHubOptions.AuthenticationPatToken)));
    }
}