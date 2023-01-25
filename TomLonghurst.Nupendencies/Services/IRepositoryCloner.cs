using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Services;

public interface IRepositoryCloner
{
    string CloneRepo(Repo repo);
}