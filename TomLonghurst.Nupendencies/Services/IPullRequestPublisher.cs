using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Services;

public interface IPullRequestPublisher
{
    Task PublishPullRequest(string clonedLocation, Repo repo, IReadOnlyList<PackageUpdateResult> packageUpdateResults);
}