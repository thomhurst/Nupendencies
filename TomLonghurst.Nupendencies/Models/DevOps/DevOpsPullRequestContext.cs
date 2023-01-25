namespace TomLonghurst.Nupendencies.Models.DevOps;

internal class DevOpsPullRequestContext
{
    public DevOpsPullRequest DevOpsPullRequest { get; set; }
    public IReadOnlyList<DevOpsPullRequestThread> PullRequestThreads { get; set; }
    public IReadOnlyList<DevOpsPullRequestIteration> Iterations { get; set; }
}