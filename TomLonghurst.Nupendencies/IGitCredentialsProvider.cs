using LibGit2Sharp;

namespace TomLonghurst.Nupendencies;

public interface IGitCredentialsProvider
{
    Credentials GetCredentials(RepositoryType repositoryType, SupportedCredentialTypes supportedCredentialTypes);
}