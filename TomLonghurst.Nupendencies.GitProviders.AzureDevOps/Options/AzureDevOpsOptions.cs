namespace TomLonghurst.Nupendencies.GitProviders.AzureDevOps.Options;

public class AzureDevOpsOptions
{
    public required string AuthenticationUsername { get; init; }
    public required string AuthenticationPatToken { get; init; }
    public required string Organization { get; init; }
    public required string ProjectName { get; init; }
    internal Guid ProjectGuid { get; init; }
    public required string[]? WorkItemIds { get; init; }
}