using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Locator;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TomLonghurst.Nupendencies.NetSdkLocator.Models;

namespace TomLonghurst.Nupendencies.NetSdkLocator;

public static class Program
{
    /// <summary>
    ///  Needs NuGet reference to <see cref="System.Runtime.CompilerServices.Unsafe"/>
    /// </summary>
    /// <param name="args"></param>
    public static void Main(string[] args)
    {
        var sdks = MSBuildLocator.QueryVisualStudioInstances()
            .Where(x => x.DiscoveryType is DiscoveryType.DotNetSdk or DiscoveryType.VisualStudioSetup)
            .Select(x => GenerateNetSdk(x))
            .ToList();

        var sdksJson = JsonConvert.SerializeObject(sdks, new JsonSerializerSettings
        {
            Converters = { new VersionConverter() }
        });

        Console.WriteLine(sdksJson);
    }

    private static NetSdk GenerateNetSdk(VisualStudioInstance x)
    {
        var fileName = x.DiscoveryType == DiscoveryType.DotNetSdk
            ? "dotnet.dll"
            : "MSBuild.exe";

        var path = Path.Combine(x.MSBuildPath, fileName);

        var is64Bit = Is64Bit(path); 
                
        return new NetSdk
        {
            Directory = x.MSBuildPath,
            Name = x.Name,
            Version = x.Version,
            IsDotNetSdk = x.DiscoveryType == DiscoveryType.DotNetSdk,
            Is64Bit = is64Bit
        };
    }

    private static bool Is64Bit(string fileName)
    {
        if (!Environment.Is64BitOperatingSystem)
        {
            return false;
        }

        var folder32Bit = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        return !Path.GetFullPath(fileName).Contains(folder32Bit);
    }
}