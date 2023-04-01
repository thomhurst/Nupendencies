using TomLonghurst.Nupendencies.Contracts;
using TomLonghurst.Nupendencies.NetSdkLocator.Models;

namespace TomLonghurst.Nupendencies.Services;

public class NetSdkProvider : INetSdkProvider
{
    private readonly ISdkFinder _sdkFinder;

    public NetSdkProvider(ISdkFinder sdkFinder)
    {
        _sdkFinder = sdkFinder;
    }
    
    public async Task<NetSdk?> GetLatestMSBuild()
    {
        var allSdks = await _sdkFinder.GetSdks();
        
        return allSdks
            ?.Where(x => !x.IsDotNetSdk)
            ?.MaxBy(x => x.Version);
    }

    public async Task<NetSdk[]> GetMSBuilds()
    {
        var allSdks = await _sdkFinder.GetSdks();

        return allSdks
            ?.Where(x => !x.IsDotNetSdk)
            ?.OrderByDescending(x => x.Version)
            ?.ToArray() ?? Array.Empty<NetSdk>();
    }

    public async Task<NetSdk?> GetLatestDotnetSdk()
    {
        var allSdks = await _sdkFinder.GetSdks();
        
        return allSdks
            ?.Where(x => x.IsDotNetSdk)
            ?.MaxBy(x => x.Version);
    }
}