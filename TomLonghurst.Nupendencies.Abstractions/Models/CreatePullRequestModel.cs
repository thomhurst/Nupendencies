namespace TomLonghurst.Nupendencies.Abstractions.Models;

public record CreatePullRequestModel
{
    public required GitRepository Repository { get; set; }
    public required UpdateReport UpdateReport { get; set; }
    public required string Title { get; set; }
    public required string Body { get; set; }
    public required string HeadBranch { get; set; }
    public required string BaseBranch { get; set; }
}