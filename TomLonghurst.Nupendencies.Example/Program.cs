// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TomLonghurst.Nupendencies.Contracts;
using TomLonghurst.Nupendencies.Extensions;
using TomLonghurst.Nupendencies.GitProviders.AzureDevOps.Extensions;
using TomLonghurst.Nupendencies.GitProviders.AzureDevOps.Options;
using TomLonghurst.Nupendencies.GitProviders.GitHub.Options;
using TomLonghurst.Nupendencies.Options;

namespace TomLonghurst.Nupendencies.Example;

public class Program
{
    public static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddUserSecrets<Program>()
            .Build();

        var nupendenciesOptions = configuration.GetSection("Nupendencies").Get<NupendenciesOptions>()!;
        var gitHubOptions = configuration.GetSection("GitHub").Get<GitHubOptions>()!;
        var azureDevOpsOptions = configuration.GetSection("AzureDevOps").Get<AzureDevOpsOptions>()!;

        var services = new ServiceCollection()
            .AddLogging(configure =>
            {
                configure.AddSimpleConsole(console =>
                {
                    console.TimestampFormat = "[HH:mm:ss] ";
                });
                configure.AddConfiguration(configuration.GetSection("Logging"));
            })
            .AddSingleton(configuration)
            .AddNupendencies(nupendenciesOptions)
            //.AddGitHubProvider(gitHubOptions)
            .AddAzureDevOpsProvider(azureDevOpsOptions)
            .PostConfigure<LoggerFilterOptions>(options =>
            {
                options.MinLevel = LogLevel.Debug;
            });

        var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });

        var nupendencyUpdater = serviceProvider.GetRequiredService<INupendencyUpdater>();

        await nupendencyUpdater.Start();
    }
}