using ModularPipelines.Context;
using ModularPipelines.Git;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace TomLonghurst.Nupendencies.Pipeline.Modules;

public class PackageFilesRemovalModule : Module
{
    protected override Task<IDictionary<string, object>?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var packageFiles = context.FileSystem.GetFiles(context.Git().RootDirectory!.Path,
            SearchOption.AllDirectories,
            path =>
                path.Extension is ".nupkg");

        foreach (var packageFile in packageFiles)
        {
            packageFile.Delete();
        }

        return NothingAsync();
    }
}
