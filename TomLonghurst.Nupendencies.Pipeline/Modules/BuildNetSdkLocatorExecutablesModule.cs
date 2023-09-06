using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace TomLonghurst.Nupendencies.Pipeline.Modules;

public class BuildNetSdkLocatorExecutablesModule : Module<List<CommandResult>>
{
    protected override async Task<List<CommandResult>?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var projectFile = context.Environment.GitRootDirectory!.GetFiles(f => f.Path.EndsWith("TomLonghurst.Nupendencies.NetSdkLocator.csproj")).First();
        
        var results = new List<CommandResult>();

        results.Add(await context.DotNet().Build(new DotNetBuildOptions
        {
            TargetPath = projectFile,
            Configuration = Configuration.Release,
            Framework = "net48",
            LogOutput = false,
        }, cancellationToken));
        
        results.Add(await context.DotNet().Build(new DotNetBuildOptions
        {
            TargetPath = projectFile,
            Configuration = Configuration.Release,
            Framework = "net7.0",
            LogOutput = false,
        }, cancellationToken));

        return results;
    }
}
