using Microsoft.Extensions.DependencyInjection;
using TomLonghurst.Microsoft.Extensions.DependencyInjection.ServiceInitialization.Extensions;
using TomLonghurst.Nupendencies.Clients;
using TomLonghurst.Nupendencies.Contracts;
using TomLonghurst.Nupendencies.Options;
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
        
        services.AddInitializers();

        services.AddSingleton<INetSdkProvider, NetSdkProvider>()
            .AddSingleton<ISdkFinder, SdkFinder>();
        
        services.AddSingleton(nupendenciesOptions);

        services.AddMemoryCache();
        
        services.AddSingleton<NuGetClient>();

        services.AddSingleton<IPreviousResultsService, PreviousResultsService>()
            .AddSingleton<IPackageVersionScanner, PackageVersionScanner>()
            .AddTransient<IRepositoryCloner, RepositoryCloner>()
            .AddTransient<IRepositoryProcessorService, RepositoryProcessorService>()
            .AddTransient<ICodeRepositoryUpdater, CodeRepositoryUpdater>()
            .AddTransient<ITargetFrameworkUpdater, TargetFrameworkUpdater>()
            .AddTransient<IUnusedDependencyRemover, UnusedDependencyRemover>()
            .AddTransient<IDependencyUpdater, DependencyUpdater>()
            .AddTransient<ISolutionBuilder, SolutionBuilder>()
            .AddTransient<INupendencyUpdater, NupendencyUpdater>()
            .AddTransient<IDirectoryService, DirectoryService>()
            .AddTransient<IIssuerRaiserService, IssuerRaiserService>()
            .AddTransient<IPullRequestPublisher, PullRequestPublisher>();

        return services;
    }
}