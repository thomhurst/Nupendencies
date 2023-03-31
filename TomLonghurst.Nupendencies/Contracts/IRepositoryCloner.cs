using TomLonghurst.Nupendencies.Abstractions.Models;

namespace TomLonghurst.Nupendencies.Contracts;

public interface IRepositoryCloner
{
    string CloneRepository(GitRepository gitRepository);
}