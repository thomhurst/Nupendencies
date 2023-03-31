using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using TomLonghurst.Microsoft.Extensions.DependencyInjection.ServiceInitialization;

namespace TomLonghurst.Nupendencies.GitProviders.AzureDevOps;

public class AzureDevOpsInitializer : IInitializer
{
    private readonly AzureDevOpsOptions _azureDevOpsOptions;
    private readonly VssConnection _vssConnection;

    public AzureDevOpsInitializer(AzureDevOpsOptions azureDevOpsOptions, VssConnection vssConnection)
    {
        _azureDevOpsOptions = azureDevOpsOptions;
        _vssConnection = vssConnection;
    }
    
    public async Task InitializeAsync()
    {
        if (_azureDevOpsOptions.ProjectGuid != default)
        {
            return;
        }

        var projects = await GetProjects();

        var foundProject = projects.SingleOrDefault(x => string.Equals(x.Name, _azureDevOpsOptions.ProjectName, StringComparison.OrdinalIgnoreCase));

        if (foundProject == null)
        {
            throw new ArgumentException($"Unique project with name '{_azureDevOpsOptions.ProjectName}' not found");
        }
        
        _azureDevOpsOptions.ProjectGuid = foundProject.Id;
    }

    private async Task<IList<TeamProjectReference>> GetProjects()
    {
        var projects = new List<TeamProjectReference>();

        string continuationToken;
        do
        {
            var projectsInIteration = await _vssConnection.GetClient<ProjectHttpClient>().GetProjects();
            
            projects.AddRange(projectsInIteration);
            
            continuationToken = projectsInIteration.ContinuationToken;
        } while (!string.IsNullOrEmpty(continuationToken));

        return projects;
    }
}