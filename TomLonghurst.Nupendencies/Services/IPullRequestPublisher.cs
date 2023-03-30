using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Services;

public interface IPullRequestPublisher
{
    Task PublishPullRequest(string clonedLocation, GitRepository gitRepository, UpdateReport updateReport);
}