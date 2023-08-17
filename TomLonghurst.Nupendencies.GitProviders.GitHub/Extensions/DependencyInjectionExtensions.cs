using Microsoft.Extensions.DependencyInjection;
using TomLonghurst.Nupendencies.Abstractions.Extensions;
using TomLonghurst.Nupendencies.GitProviders.GitHub.Clients;
using TomLonghurst.Nupendencies.GitProviders.GitHub.Options;
using TomLonghurst.Nupendencies.GitProviders.GitHub.Services;

namespace TomLonghurst.Nupendencies.GitProviders.GitHub.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddGitHubProvider(this IServiceCollection services,
        GitHubOptions options)
    {
        services.AddSingleton(options);

        services.AddSingleton<IGitHubClientProvider, GitHubClientProvider>()
            .AddGitProvider<GitHubProvider>()
            .AddTransient<IGitHubIssueService, GitHubIssueService>()
            .AddTransient<IGitHubPullRequestService, GitHubPullRequestService>()
            .AddTransient<IGitHubRepositoriesProvider, GitHubRepositoriesProvider>();

        return services;
    }
}