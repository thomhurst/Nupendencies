namespace TomLonghurst.Nupendencies.Models;

public class GitRepository
{
    public GitRepository(RepositoryType repositoryType)
    {
        RepositoryType = repositoryType;
    }

    public string Owner { get; set; }

    public string Name { get; set; }

    public string Id { get; set; }

    public bool IsDisabled { get; set; }

    public List<Iss> Issues { get; set; }

    private string _gitUrl;

    public string GitUrl
    {
        get => _gitUrl;
        set => _gitUrl = GetGitUrl(value);
    }

    private string GetGitUrl(string value)
    {
        if (RepositoryType == RepositoryType.Github)
        {
            return value.Replace("git@github.com:", "https://github.com/");
        }

        return value;
    }

    public string MainBranch { get; set; }
    public RepositoryType RepositoryType { get; }
}