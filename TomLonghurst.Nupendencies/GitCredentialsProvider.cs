using LibGit2Sharp;
using TomLonghurst.Nupendencies.Abstractions.Contracts;
using TomLonghurst.Nupendencies.Options;

namespace TomLonghurst.Nupendencies;

public class GitCredentialsProvider : IGitCredentialsProvider
{
    private readonly NupendenciesOptions _nupendenciesOptions;

    public GitCredentialsProvider(NupendenciesOptions nupendenciesOptions)
    {
        _nupendenciesOptions = nupendenciesOptions;
    }
    
    public Credentials GetCredentials(RepositoryType repositoryType, SupportedCredentialTypes supportedCredentialTypes)
    {
        var pat = repositoryType == RepositoryType.GitHub
            ? _nupendenciesOptions.GitHubOptions.PatToken
            : _nupendenciesOptions.AzureDevOpsOptions.PatToken;

        string username = null;
        string password;
        if (pat.Contains(':'))
        {
            var splitPat = pat.Split(':');
            username = splitPat[0];
            password = splitPat[1];   
        }
        else
        {
            password = pat;
        }

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