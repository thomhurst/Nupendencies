using System.Text.Json.Serialization;

namespace TomLonghurst.Nupendencies.Models.DevOps;

public record DevOpsPullRequestThreadProperties(
    [property: JsonPropertyName("CodeReviewThreadType")] DevOpsPullRequestThreadPropertyValue? CodeReviewThreadType
);