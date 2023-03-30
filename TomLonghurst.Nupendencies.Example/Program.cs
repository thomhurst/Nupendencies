// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TomLonghurst.Nupendencies;
using TomLonghurst.Nupendencies.Extensions;
using TomLonghurst.Nupendencies.Services;

public class Program
{
    public static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<Program>()
                .Build();

            var nupendenciesOptions = configuration.GetSection("Nupendencies").Get<NupendenciesOptions>();
            
            var services = new ServiceCollection()
                .AddLogging(configure =>
                {
                    configure.AddConsole(console =>
                    {
                        console.TimestampFormat = "[HH:mm:ss] ";
                    });
                    configure.AddConfiguration(configuration.GetSection("Logging"));
                })
                .AddSingleton(configuration)
                .AddNupendencies(nupendenciesOptions)
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