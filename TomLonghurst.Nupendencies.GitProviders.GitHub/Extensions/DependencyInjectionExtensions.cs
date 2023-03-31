using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using TomLonghurst.Nupendencies.Abstractions.Extensions;
using TomLonghurst.Nupendencies.GitProviders.GitHub.Clients;
using TomLonghurst.Nupendencies.GitProviders.GitHub.Options;

namespace TomLonghurst.Nupendencies.GitProviders.GitHub.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddGitHubProvider(this IServiceCollection services,
        GitHubOptions options)
    {
        services.AddSingleton(options);
        services.AddHttpClient<GitHubHttpClient>(client =>
            {
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("pull-request-scanner", Assembly.GetAssembly(typeof(GitHubProvider))?.GetName().Version?.ToString()));

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(
                        $"{options.AuthenticationUsername}:{options.AuthenticationPatToken}")));
                client.BaseAddress = new Uri("https://api.github.com/");
            });

        services.AddSingleton<IGitHubGraphQlClientProvider, GitHubGraphQlClientProvider>()
            .AddGitProvider<GitHubProvider>()
            .AddTransient<IGitHubIssuerService, GitHubIssuerService>()
            .AddTransient<IGitHubPullRequestService, GitHubPullRequestService>();

        return services;
    }
}