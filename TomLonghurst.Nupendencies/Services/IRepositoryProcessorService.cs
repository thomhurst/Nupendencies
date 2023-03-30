using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Services;

public interface IRepositoryProcessorService
{
    Task Process(GitRepository gitRepository);
}