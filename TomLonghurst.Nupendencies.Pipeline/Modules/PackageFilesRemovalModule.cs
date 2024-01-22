using ModularPipelines.Context;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Modules;

namespace TomLonghurst.Nupendencies.Pipeline.Modules;

public class PackageFilesRemovalModule : Module
{
    protected override async Task<IDictionary<string, object>?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var packageFiles = context.Git().RootDirectory.GetFiles(path => path.Extension is ".nupkg");

        foreach (var packageFile in packageFiles)
        {
            packageFile.Delete();
        }

        return await NothingAsync();
    }
}
