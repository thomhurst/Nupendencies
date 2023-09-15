using ModularPipelines.Context;
using ModularPipelines.DotNet;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Git;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace TomLonghurst.Nupendencies.Pipeline.Modules;

public class RunUnitTestsModule : Module<List<DotNetTestResult>>
{
    protected override async Task<List<DotNetTestResult>?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var results = new List<DotNetTestResult>();

        foreach (var unitTestProjectFile in context
                     .Git().RootDirectory!
                     .GetFiles(file => file.Path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                                       && file.Path.Contains("UnitTests", StringComparison.OrdinalIgnoreCase)))
        {
            results.Add(await context.DotNet().Test(new DotNetTestOptions
            {
                TargetPath = unitTestProjectFile.Path,
                LogOutput = false
            }, cancellationToken));
        }

        return results;
    }
}
