using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using TomLonghurst.Microsoft.Extensions.DependencyInjection.ServiceInitialization.Extensions;
using TomLonghurst.Nupendencies.Clients;
using TomLonghurst.Nupendencies.Services;
using Build = Microsoft.Build;

namespace TomLonghurst.Nupendencies.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddNupendencies(this IServiceCollection services, NupendenciesOptions nupendenciesOptions)
    {
        var instance = Build.Locator.MSBuildLocator.QueryVisualStudioInstances()
            .MaxBy(x => x.Version);
        
        Build.Locator.MSBuildLocator.RegisterInstance(instance);

        services.AddSingleton(nupendenciesOptions.AzureDevOpsOptions);

        services.AddInitializers()
            .AddSingleton<AzureDevOpsInitializer>();

        services.AddSingleton<INetSdkProvider, NetSdkProvider>()
            .AddSingleton<ISdkFinder, SdkFinder>();
        
        services.AddSingleton(nupendenciesOptions);

        services.AddMemoryCache();

        // services.AddLogging(configure =>
        // {
        //     configure.AddConsole();
        // });

        services.AddHttpClient<GithubHttpClient>(client =>
        {
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("pull-request-scanner", Assembly.GetAssembly(typeof(INupendencyUpdater)).GetName().Version.ToString()));

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.ASCII.GetBytes(nupendenciesOptions.GithubOptions.PatToken)));
            client.BaseAddress = new Uri("https://api.github.com/");
        });

        services.AddSingleton(sp =>
        {
            var azureDevOpsOptions = nupendenciesOptions.AzureDevOpsOptions;

            var uri = new UriBuilder($"https://dev.azure.com")
            {
                Path = $"/{azureDevOpsOptions.Organization}"
            }.Uri;

            return new VssConnection(uri, new VssBasicCredential(string.Empty, azureDevOpsOptions.PatToken));
        });

        services.AddSingleton<NuGetClient>();
            
        services.AddSingleton<IGithubGraphQlClientProvider, GithubGraphQlClientProvider>()
            .AddSingleton<IPreviousResultsService, PreviousResultsService>()
            .AddSingleton<IPackageVersionScanner, PackageVersionScanner>()
            .AddTransient<IGitCredentialsProvider, GitCredentialsProvider>()
            .AddTransient<IRepositoryCloner, RepositoryCloner>()
            .AddTransient<IRepositoryProcessorService, RepositoryProcessorService>()
            .AddTransient<ICodeRepositoryUpdater, CodeRepositoryUpdater>()
            .AddSingleton<ITargetFrameworkUpdater, TargetFrameworkUpdater>()
            .AddTransient<ISolutionBuilder, SolutionBuilder>()
            .AddTransient<INupendencyUpdater, NupendencyUpdater>()
            .AddTransient<IDirectoryService, DirectoryService>()
            
            .AddSingleton<IGithubGetService, GithubGetService>()
            .AddSingleton<IDevOpsGetService, DevOpsGetService>()
            
            .AddTransient<IIssuerRaiserService, GithubIssuerRaiserService>()
            .AddTransient<IIssuerRaiserService, DevOpsIssuerRaiserService>()
            
            .AddTransient<IPullRequestPublisher, GithubPullRequestPublisher>()
            .AddTransient<IPullRequestPublisher, DevOpsPullRequestPublisher>();
        
        return services;
    }
}