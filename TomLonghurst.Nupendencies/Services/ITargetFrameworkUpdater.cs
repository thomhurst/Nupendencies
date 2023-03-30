namespace TomLonghurst.Nupendencies.Services;

public interface ITargetFrameworkUpdater
{
    Task TryUpdateTargetFramework(CodeRepository repository);
}