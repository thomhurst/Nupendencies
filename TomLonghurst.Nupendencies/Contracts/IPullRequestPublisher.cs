using TomLonghurst.Nupendencies.Abstractions.Models;

namespace TomLonghurst.Nupendencies.Contracts;

public interface IPullRequestPublisher
{
    Task PublishPullRequest(string clonedLocation, GitRepository gitRepository, UpdateReport updateReport);
}