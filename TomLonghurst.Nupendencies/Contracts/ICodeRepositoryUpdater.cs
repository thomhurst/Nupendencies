using TomLonghurst.Nupendencies.Services;

namespace TomLonghurst.Nupendencies.Contracts;

public interface ICodeRepositoryUpdater
{
    Task<UpdateReport> UpdateRepository(CodeRepository repository);
}