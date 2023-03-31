using TomLonghurst.Nupendencies.Abstractions.Models;

namespace TomLonghurst.Nupendencies.Contracts;

public interface ICodeRepositoryUpdater
{
    Task<UpdateReport> UpdateRepository(CodeRepository repository);
}