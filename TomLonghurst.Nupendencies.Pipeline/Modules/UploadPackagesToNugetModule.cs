using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using ModularPipelines.Enums;
using ModularPipelines.NuGet.Extensions;
using ModularPipelines.NuGet.Options;
using TomLonghurst.Nupendencies.Pipeline.Settings;

namespace TomLonghurst.Nupendencies.Pipeline.Modules;

[DependsOn<RunUnitTestsModule>]
[DependsOn<PackagePathsParserModule>]
public class UploadPackagesToNugetModule : Module<CommandResult[]>
{
    private readonly IOptions<NuGetSettings> _options;

    public UploadPackagesToNugetModule(IOptions<NuGetSettings> options)
    {
        _options = options;
    }

    protected override async Task OnBeforeExecute(IPipelineContext context)
    {
        var packagePaths = await GetModule<PackagePathsParserModule>();

        foreach (var packagePath in packagePaths.Value!)
        {
            context.Logger.LogInformation("Uploading {File}", packagePath);
        }

        await base.OnBeforeExecute(context);
    }

    protected override async Task<SkipDecision> ShouldSkip(IPipelineContext context)
    {
        var gitVersionInfo = await context.Git().Versioning.GetGitVersioningInformation();

        if (gitVersionInfo.BranchName != "main")
        {
            return true;
        }
        
        var publishPackages =
            context.Environment.EnvironmentVariables.GetEnvironmentVariable("PUBLISH_PACKAGES")!;

        if (!bool.TryParse(publishPackages, out var shouldPublishPackages) || !shouldPublishPackages)
        {
            return true;
        }

        return false;
    }

    protected override async Task<CommandResult[]?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(_options.Value.ApiKey);

        var gitVersionInformation = await context.Git().Versioning.GetGitVersioningInformation();

        if (gitVersionInformation.BranchName != "main")
        {
            return await NothingAsync();
        }

        var packagePaths = await GetModule<PackagePathsParserModule>();

        return await context.NuGet()
            .UploadPackages(new NuGetUploadOptions(packagePaths.Value!.AsPaths(), new Uri("https://api.nuget.org/v3/index.json"))
            {
                ApiKey = _options.Value.ApiKey!,
                NoSymbols = true
            });
    }
}
