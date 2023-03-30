using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Services;

public interface ICodeRepositoryUpdater
{
    Task<UpdateReport> UpdateRepository(CodeRepository repository);
}