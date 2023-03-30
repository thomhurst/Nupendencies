using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Services;

public interface IRepositoryCloner
{
    string CloneRepository(GitRepository gitRepository);
}