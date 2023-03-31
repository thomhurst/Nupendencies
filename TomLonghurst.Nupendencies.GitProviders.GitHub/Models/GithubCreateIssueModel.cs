﻿using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace TomLonghurst.Nupendencies.GitProviders.GitHub.Models;

public class GitHubCreateIssueModel
{
    [JsonPropertyName("owner"), JsonProperty("owner")]
    public string Owner { get; init; }
    
    [JsonPropertyName("repo"), JsonProperty("repo")]
    public string Repo { get; init; }
    
    [JsonPropertyName("title"), JsonProperty("title")]
    public string Title { get; init; }
    
    [JsonPropertyName("body"), JsonProperty("body")]
    public string Body { get; init; }
}