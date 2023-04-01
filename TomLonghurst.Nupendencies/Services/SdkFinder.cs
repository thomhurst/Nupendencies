using System.Reflection;
using System.Text.Json;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.Extensions.Logging;
using TomLonghurst.Nupendencies.Contracts;
using TomLonghurst.Nupendencies.NetSdkLocator.Models;

namespace TomLonghurst.Nupendencies.Services;

public class SdkFinder : ISdkFinder
{
    private readonly ILogger<SdkFinder> _logger;
    private NetSdk[]? _cachedSdks;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public SdkFinder(ILogger<SdkFinder> logger)
    {
        _logger = logger;
    }
    
    public async Task<NetSdk[]> GetSdks()
    {
        if (_cachedSdks != null)
        {
            return _cachedSdks;
        }

        await _lock.WaitAsync();

        try
        {
            if (_cachedSdks != null)
            {
                return _cachedSdks;
            }
            
            var workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

            var netSdkLocatorDirectory = Path.Combine(workingDirectory, "NetSdkLocator");

            var resultNetCore = await ExecuteDotnetLocator(Path.Combine(netSdkLocatorDirectory, "net7.0"));
            var resultNetFramework = await ExecuteMsBuildLocator(Path.Combine(netSdkLocatorDirectory, "net48"));

            _cachedSdks = resultNetCore.Concat(resultNetFramework).ToArray();
        
            foreach (var sdk in _cachedSdks)
            {
                _logger.LogInformation("Found SDK: {Sdk}", sdk);
            }

            return _cachedSdks = resultNetCore.Concat(resultNetFramework).ToArray();
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<NetSdk[]> ExecuteDotnetLocator(string buildLocatorDirectory)
    {
        var result = await Cli.Wrap("dotnet")
            .WithWorkingDirectory(buildLocatorDirectory)
            .WithArguments($"\"TomLonghurst.Nupendencies.NetSdkLocator.dll\"")
            .WithEnvironmentVariables(new Dictionary<string, string?>
            {
                ["MsBuildExtensionPath"] = null,
                ["MsBuildSDKsPath"] = null,
                ["MSBUILD_EXE_PATH"] = null
            })
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();

        if (result.ExitCode != 0)
        {
            _logger.LogError("Error retrieving Dotnet exe: {Output}", result.StandardOutput);
            return Array.Empty<NetSdk>();
        }

        return JsonSerializer.Deserialize<NetSdk[]>(result.StandardOutput) ?? Array.Empty<NetSdk>();
    }
    
    private async Task<NetSdk[]> ExecuteMsBuildLocator(string buildLocatorDirectory)
    {
        var result = await Cli.Wrap("Powershell")
            .WithArguments("powershell.exe -ExecutionPolicy Bypass -Command \".\\TomLonghurst.Nupendencies.NetSdkLocator.exe\"")
            .WithWorkingDirectory(buildLocatorDirectory)
            .WithEnvironmentVariables(new Dictionary<string, string?>
            {
                ["MsBuildExtensionPath"] = null,
                ["MsBuildSDKsPath"] = null,
                ["MSBUILD_EXE_PATH"] = null
            })
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();
        
        if (result.ExitCode != 0)
        {
            _logger.LogError("Error retrieving {Version} SDK: {Output}", result.StandardOutput);
            return Array.Empty<NetSdk>();
        }

        return JsonSerializer.Deserialize<NetSdk[]>(result.StandardOutput) ?? Array.Empty<NetSdk>();
    }
}