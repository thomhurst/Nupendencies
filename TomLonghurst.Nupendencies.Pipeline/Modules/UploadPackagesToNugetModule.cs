using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using ModularPipelines.NuGet.Extensions;
using ModularPipelines.NuGet.Options;
using TomLonghurst.Nupendencies.Pipeline.Settings;

namespace TomLonghurst.Nupendencies.Pipeline.Modules;

[DependsOn<RunUnitTestsModule>]
[DependsOn<PackagePathsParserModule>]
public class UploadPackagesToNugetModule : Module<List<CommandResult>>
{
    private readonly IOptions<NuGetSettings> _options;

    public UploadPackagesToNugetModule(IOptions<NuGetSettings> options)
    {
        ArgumentNullException.ThrowIfNull(options.Value.ApiKey);
        _options = options;
    }

    protected override async Task OnBeforeExecute(IModuleContext context)
    {
        var packagePaths = await GetModule<PackagePathsParserModule>();

        foreach (var packagePath in packagePaths.Value!)
        {
            context.Logger.LogInformation("Uploading {File}", packagePath);
        }

        await base.OnBeforeExecute(context);
    }

    protected override async Task<bool> ShouldSkip(IModuleContext context)
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

    protected override async Task<ModuleResult<List<CommandResult>>?> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
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