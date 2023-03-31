using TomLonghurst.Nupendencies.Models;
using TomLonghurst.Nupendencies.Services;

namespace TomLonghurst.Nupendencies.Contracts;

public interface IPullRequestPublisher
{
    Task PublishPullRequest(string clonedLocation, GitRepository gitRepository, UpdateReport updateReport);
}