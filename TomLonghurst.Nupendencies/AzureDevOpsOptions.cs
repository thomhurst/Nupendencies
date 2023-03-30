﻿namespace TomLonghurst.Nupendencies;

public class AzureDevOpsOptions
{
    public string PatToken { get; set; }
    public string Organization { get; set; }
    public string ProjectName { get; set; }
    internal Guid ProjectGuid { get; set; }
    public string[]? WorkItemIds { get; set; }
    public string Username { get; set; }
}