namespace TomLonghurst.Nupendencies.Abstractions.Contracts;

public interface IGitCredentialsProvider
{
    Credentials GetCredentials(RepositoryType repositoryType, SupportedCredentialTypes supportedCredentialTypes);
}