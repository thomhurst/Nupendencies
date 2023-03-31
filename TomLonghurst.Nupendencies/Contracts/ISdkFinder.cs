using TomLonghurst.Nupendencies.NetSdkLocator.Models;

namespace TomLonghurst.Nupendencies.Contracts;

public interface ISdkFinder
{
    Task<NetSdk[]> GetSdks();
}