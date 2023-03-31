using TomLonghurst.Nupendencies.Abstractions.Models;
using GitRepository = TomLonghurst.Nupendencies.Models.GitRepository;

namespace TomLonghurst.Nupendencies.Contracts;

public interface IPullRequestPublisher
{
    Task PublishPullRequest(string clonedLocation, GitRepository gitRepository, UpdateReport updateReport);
}