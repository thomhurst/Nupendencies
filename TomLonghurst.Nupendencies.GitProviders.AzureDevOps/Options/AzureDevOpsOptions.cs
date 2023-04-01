namespace TomLonghurst.Nupendencies.GitProviders.AzureDevOps.Options;

public class AzureDevOpsOptions
{
    public required string AuthenticationUsername { get; set; }
    public required string AuthenticationPatToken { get; set; }
    public required string Organization { get; set; }
    public required string ProjectName { get; set; }
    internal Guid ProjectGuid { get; set; }
    public required string[]? WorkItemIdsToAttachToPullRequests { get; set; }
}