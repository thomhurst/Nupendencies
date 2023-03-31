using System.Reflection;
using Octokit.GraphQL;
using TomLonghurst.Nupendencies.Options;

namespace TomLonghurst.Nupendencies.Clients;

internal class GithubGraphQlClientProvider : IGithubGraphQlClientProvider
{
    public Connection GithubGraphQlClient { get; }

    public GithubGraphQlClientProvider(NupendenciesOptions nupendenciesOptions)
    {
        var version = Assembly.GetAssembly(typeof(GithubGraphQlClientProvider))?.GetName()?.Version?.ToString() ?? "1.0";

        var accessToken = nupendenciesOptions.GithubOptions.PatToken;

        if (accessToken.Contains(':'))
        {
            accessToken = accessToken.Split(':').Last();
        }
        
        GithubGraphQlClient = new Connection(new ProductHeaderValue(Constants.AppName, version), accessToken);
    }
}