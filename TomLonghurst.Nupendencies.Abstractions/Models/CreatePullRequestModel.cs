namespace TomLonghurst.Nupendencies.Abstractions.Models;

public record CreatePullRequestModel
{
    public required GitRepository Repository { get; init; }
    public required UpdateReport UpdateReport { get; init; }
    public required string Title { get; init; }
    public required string Body { get; init; }
    public required string HeadBranch { get; init; }
    public required string BaseBranch { get; init; }
}