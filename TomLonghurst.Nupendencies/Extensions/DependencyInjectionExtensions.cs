using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using TomLonghurst.Nupendencies.Clients;
using TomLonghurst.Nupendencies.Services;

namespace TomLonghurst.Nupendencies.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddNupendencies(this IServiceCollection services, NupendenciesOptions nupendenciesOptions)
    {
        var instance = Microsoft.Build.Locator.MSBuildLocator.QueryVisualStudioInstances()
            .MaxBy(x => x.Version);
        
        Microsoft.Build.Locator.MSBuildLocator.RegisterInstance(instance);
        
        services.AddSingleton<INetSdkProvider, NetSdkProvider>()
            .AddSingleton<ISdkFinder, SdkFinder>()
            .AddSingleton<IRepositoryTreeGenerator, RepositoryTreeGenerator>();

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
            
        services.AddHttpClient<DevOpsHttpClient>(client =>
        {
            var azureDevOpsOptions = nupendenciesOptions.AzureDevOpsOptions;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.ASCII.GetBytes(azureDevOpsOptions.PatToken)));
            client.BaseAddress = new Uri($"https://dev.azure.com/{azureDevOpsOptions.Organization}/{azureDevOpsOptions.Project}/_apis/");
        });
        
        services.AddSingleton<NuGetClient>();
            
        services.AddSingleton<IGithubGraphQlClientProvider, GithubGraphQlClientProvider>()
            .AddSingleton<IPreviousResultsService, PreviousResultsService>()
            .AddSingleton<IPackageVersionScanner, PackageVersionScanner>()
            .AddTransient<IGitCredentialsProvider, GitCredentialsProvider>()
            .AddTransient<IRepositoryCloner, RepositoryCloner>()
            .AddTransient<IRepositoryProcessorService, RepositoryProcessorService>()
            .AddTransient<ISolutionUpdater, SolutionUpdater>()
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