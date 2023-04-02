using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using TomLonghurst.Nupendencies.Abstractions.Extensions;
using TomLonghurst.Nupendencies.Abstractions.Models;
using TomLonghurst.Nupendencies.Clients;
using TomLonghurst.Nupendencies.Contracts;

namespace TomLonghurst.Nupendencies.Services;

public class CodeRepositoryUpdater : ICodeRepositoryUpdater
{
    private readonly NuGetClient _nuGetClient;
    private readonly ITargetFrameworkUpdater _targetFrameworkUpdater;
    private readonly IUnusedDependencyRemover _unusedDependencyRemover;
    private readonly IDependencyUpdater _dependencyUpdater;
    private readonly ISolutionBuilder _solutionBuilder;
    private readonly ILogger<CodeRepositoryUpdater> _logger;

    public CodeRepositoryUpdater(NuGetClient nuGetClient,
        ITargetFrameworkUpdater targetFrameworkUpdater,
        IUnusedDependencyRemover unusedDependencyRemover,
        IDependencyUpdater dependencyUpdater,
        ISolutionBuilder solutionBuilder,
        ILogger<CodeRepositoryUpdater> logger)
    {
        _nuGetClient = nuGetClient;
        _targetFrameworkUpdater = targetFrameworkUpdater;
        _unusedDependencyRemover = unusedDependencyRemover;
        _dependencyUpdater = dependencyUpdater;
        _solutionBuilder = solutionBuilder;
        _logger = logger;
    }

    public async Task<UpdateReport> UpdateRepository(CodeRepository repository)
    {
        _logger.LogInformation("Project Tree: {ProjectTree}", repository);

        var targetFrameworkUpdateResult = await _targetFrameworkUpdater.TryUpdateTargetFramework(repository);
        
        var removedProjectRemovalResults = await _unusedDependencyRemover.TryDeleteRedundantProjectReferences(repository).ToListAsync();

        var removedPackageReferencesResults = await _unusedDependencyRemover.TryDeleteRedundantPackageReferences(repository).ToListAsync();
        
        var updateResults = await _dependencyUpdater.TryUpdatePackages(repository);

        await UpdateWebConfigFiles(repository);

        return new UpdateReport(
            targetFrameworkUpdateResult,
            removedPackageReferencesResults,
            removedProjectRemovalResults,
            updateResults
        );
    }

    private async Task UpdateWebConfigFiles(CodeRepository codeRepository)
    {
        foreach (var webConfigFile in Directory
                     .GetFiles(codeRepository.RepositoryPath, "Web.config", SearchOption.AllDirectories)
                     .Distinct())
        {
            TryRegenerateBindings(webConfigFile);
        }

        // One last build in-case we need to generate binding redirects etc
        var upperMostProjects = codeRepository.AllProjects.GetProjectsToBuild();
        
        await _solutionBuilder.BuildProjects(upperMostProjects, "clean");
        await _solutionBuilder.BuildProjects(upperMostProjects);
    }

    private void TryRegenerateBindings(string webConfigFile)
    {
        try
        {
            var xdoc = XDocument.Load(webConfigFile);

            xdoc.Descendants("assemblyBinding")
                .ToList()
                .ForEach(x => x.Remove());

            xdoc.Save(webConfigFile);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error regenerating bindings");
        }
    }
}