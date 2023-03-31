using TomLonghurst.Nupendencies.Abstractions.Models;

namespace TomLonghurst.Nupendencies.Contracts;

public interface ITargetFrameworkUpdater
{
    Task<TargetFrameworkUpdateResult> TryUpdateTargetFramework(CodeRepository repository);
}