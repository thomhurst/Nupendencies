using Microsoft.Build.Construction;

namespace TomLonghurst.Nupendencies;

public interface IRepositoryTreeGenerator
{
    public Task<RepositoryProjectTree> Generate(ProjectRootElement[] projects);
}