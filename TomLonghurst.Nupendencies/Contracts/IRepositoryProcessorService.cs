using TomLonghurst.Nupendencies.Abstractions.Models;

namespace TomLonghurst.Nupendencies.Contracts;

public interface IRepositoryProcessorService
{
    Task Process(GitRepository gitRepository);
}