using TomLonghurst.Nupendencies.Services;

namespace TomLonghurst.Nupendencies.Contracts;

public interface ITargetFrameworkUpdater
{
    Task<TargetFrameworkUpdateResult> TryUpdateTargetFramework(CodeRepository repository);
}