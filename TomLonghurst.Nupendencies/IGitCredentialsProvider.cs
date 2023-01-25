using LibGit2Sharp;

namespace TomLonghurst.Nupendencies;

public interface IGitCredentialsProvider
{
    Credentials GetCredentials(RepositoryType repositoryType, SupportedCredentialTypes supportedCredentialTypes);
}

public class GitCredentialsProvider : IGitCredentialsProvider
{
    private readonly NupendenciesOptions _nupendenciesOptions;

    public GitCredentialsProvider(NupendenciesOptions nupendenciesOptions)
    {
        _nupendenciesOptions = nupendenciesOptions;
    }
    
    public Credentials GetCredentials(RepositoryType repositoryType, SupportedCredentialTypes supportedCredentialTypes)
    {
        var pat = repositoryType == RepositoryType.Github
            ? _nupendenciesOptions.GithubOptions.PatToken
            : _nupendenciesOptions.AzureDevOpsOptions.PatToken;

        var splitPat = pat.Split(':');
        var username = splitPat[0];
        var password = splitPat[1];

        if (string.IsNullOrEmpty(username))
        {
            username = _nupendenciesOptions.AzureDevOpsOptions.Username;
        }

        if (string.IsNullOrEmpty(username))
        {
            throw new ArgumentException("Azure DevOps username required", nameof(username));
        }
        
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("Azure DevOps username required", nameof(password));
        }
            
        return new UsernamePasswordCredentials
        {
            Username = username,
            Password = password
        };
    }
}