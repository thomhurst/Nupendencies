using Microsoft.Extensions.DependencyInjection;
using TomLonghurst.Nupendencies.Abstractions.Contracts;

namespace TomLonghurst.Nupendencies.Abstractions.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddGitProvider<TGitProvider>(this IServiceCollection services)
        where TGitProvider : class, IGitProvider
    {
        return services.AddTransient<IGitProvider, TGitProvider>();
    }

}