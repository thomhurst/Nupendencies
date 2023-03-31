using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using TomLonghurst.Nupendencies.Abstractions.Extensions;
using TomLonghurst.Nupendencies.GitProviders.AzureDevOps.Options;

namespace TomLonghurst.Nupendencies.GitProviders.AzureDevOps.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddAzureDevOpsProvider(this IServiceCollection services,
        AzureDevOpsOptions options)
    {
        services.AddSingleton(options)
            .AddSingleton<AzureDevOpsInitializer>();

        services.AddSingleton(_ =>
        {
            var uri = new UriBuilder($"https://dev.azure.com")
            {
                Path = $"/{options.Organization}"
            }.Uri;

            return new VssConnection(uri, new VssBasicCredential(string.Empty, options.AuthenticationPatToken));
        });

        services.AddGitProvider<AzureDevOpsProvider>()
            .AddTransient<IDevOpsPullRequestService, DevOpsPullRequestService>();

        return services;
    }
}