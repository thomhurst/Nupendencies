using TomLonghurst.Nupendencies.NetSdkLocator.Models;

namespace TomLonghurst.Nupendencies;

public interface ISdkFinder
{
    Task<NetSdk[]> GetSdks();
}