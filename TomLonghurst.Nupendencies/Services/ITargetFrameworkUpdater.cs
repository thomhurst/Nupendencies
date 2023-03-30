namespace TomLonghurst.Nupendencies.Services;

public interface ITargetFrameworkUpdater
{
    Task<TargetFrameworkUpdateResult> TryUpdateTargetFramework(CodeRepository repository);
}