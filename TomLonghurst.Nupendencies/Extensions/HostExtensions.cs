using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TomLonghurst.Nupendencies.Contracts;

namespace TomLonghurst.Nupendencies.Extensions;

public static class HostExtensions
{
    public static Task ExecuteNupendencies(this IHost host)
    {
        return host.Services.GetRequiredService<INupendencyUpdater>().Start();
    }
}