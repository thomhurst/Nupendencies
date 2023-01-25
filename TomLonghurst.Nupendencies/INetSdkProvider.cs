using TomLonghurst.Nupendencies.NetSdkLocator.Models;

namespace TomLonghurst.Nupendencies;

public interface INetSdkProvider
{
    public Task<NetSdk?> GetLatestMSBuild();
    
    public Task<NetSdk[]> GetMSBuilds();


    public Task<NetSdk?> GetLatestDotnetSdk();
}