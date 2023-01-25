﻿using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace TomLonghurst.Nupendencies.Models.Github;

public class GithubCreateIssueModel
{
    [JsonPropertyName("owner"), JsonProperty("owner")]
    public string Owner { get; set; }
    
    [JsonPropertyName("repo"), JsonProperty("repo")]
    public string Repo { get; set; }
    
    [JsonPropertyName("title"), JsonProperty("title")]
    public string Title { get; set; }
    
    [JsonPropertyName("body"), JsonProperty("body")]
    public string Body { get; set; }
}