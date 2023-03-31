using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Contracts;

public interface IRepositoryCloner
{
    string CloneRepository(GitRepository gitRepository);
}