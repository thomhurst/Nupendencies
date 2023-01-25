namespace TomLonghurst.Nupendencies;

public class AzureDevOpsOptions
{
    public string PatToken { get; set; }
    public string Organization { get; set; }
    public string Project { get; set; }
    public string[] WorkItemIds { get; set; }
    public string Username { get; set; }
}