using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;
using CommandLine;
using Microsoft.Build.Locator;
using Newtonsoft.Json;

namespace TomLonghurst.Nupendencies.Builder.MSBuild;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var instance = RegisterMsBuild();

        if (instance == null)
        {
            Console.Write("No MSBuild.exe found. Cannot build this project.");
            return -1;
        }

        var options = ParseOptions(args);

        return await Process(options, instance);
    }

    private static async Task<int> Process(Options options, VisualStudioInstance instance)
    {
        var command = Cli.Wrap(Path.Combine(instance.MSBuildPath, "MSBuild.exe"))
            .WithWorkingDirectory(Path.GetDirectoryName(options.ProjectFilePath))
            .WithArguments($"\"{Path.GetFileName(options.ProjectFilePath)}\" /restore -t:build /p:Configuration=Release")
            .WithValidation(CommandResultValidation.None);

        if (!string.IsNullOrEmpty(options.AzureArtifactsCredentialsJson))
        {
            command = command.WithEnvironmentVariables(new Dictionary<string, string>
            {
                ["VSS_NUGET_EXTERNAL_FEED_ENDPOINTS"] = options.AzureArtifactsCredentialsJson
            });
        }

        var result = await command.ExecuteBufferedAsync();

        Console.WriteLine(result.StandardOutput);

        return result.ExitCode;
    }

    private static VisualStudioInstance RegisterMsBuild()
    {
        var instances = MSBuildLocator.QueryVisualStudioInstances();
            
        Console.WriteLine(JsonConvert.SerializeObject(instances));
            
        var instance = instances
            .OrderByDescending(x => x.Version)
            .FirstOrDefault();

        MSBuildLocator.RegisterInstance(instance);
            
        return instance;
    }

    private static Options ParseOptions(string[] args)
    {
        var options = Parser.Default.ParseArguments<Options>(args).Value;
            
        if (!string.IsNullOrEmpty(options.AzureArtifactsCredentialsJson))
        {
            Environment.SetEnvironmentVariable("VSS_NUGET_EXTERNAL_FEED_ENDPOINTS", options.AzureArtifactsCredentialsJson);   
        }

        return options;
    }
}