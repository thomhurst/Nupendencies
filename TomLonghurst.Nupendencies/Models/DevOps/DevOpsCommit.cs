using System.Text.Json.Serialization;

namespace TomLonghurst.Nupendencies.Models.DevOps;

public record DevOpsCommit(
    [property: JsonPropertyName("committer")] DevOpsCommitter Committer
    );