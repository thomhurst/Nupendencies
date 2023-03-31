using LibGit2Sharp;

namespace TomLonghurst.Nupendencies.Contracts;

public interface IGitCredentialsProvider
{
    Credentials GetCredentials(RepositoryType repositoryType, SupportedCredentialTypes supportedCredentialTypes);
}