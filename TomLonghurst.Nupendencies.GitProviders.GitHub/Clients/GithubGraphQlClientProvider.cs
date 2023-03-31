using System.Reflection;
using Microsoft.VisualBasic;
using Octokit.GraphQL;
using TomLonghurst.Nupendencies.Abstractions;
using TomLonghurst.Nupendencies.GitProviders.GitHub.Options;

namespace TomLonghurst.Nupendencies.GitProviders.GitHub.Clients;

internal class GitHubGraphQlClientProvider : IGitHubGraphQlClientProvider
{
    public Connection GitHubGraphQlClient { get; }

    public GitHubGraphQlClientProvider(GitHubOptions gitHubOptions)
    {
        var version = Assembly.GetAssembly(typeof(GitHubGraphQlClientProvider))?.GetName()?.Version?.ToString() ?? "1.0";

        var accessToken = gitHubOptions.PatToken;

        if (accessToken.Contains(':'))
        {
            accessToken = accessToken.Split(':').Last();
        }
        
        GitHubGraphQlClient = new Connection(new ProductHeaderValue(NupendencyConstants.AppName, version), accessToken);
    }
}