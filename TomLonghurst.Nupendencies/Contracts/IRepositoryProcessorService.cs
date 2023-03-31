using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Contracts;

public interface IRepositoryProcessorService
{
    Task Process(GitRepository gitRepository);
}