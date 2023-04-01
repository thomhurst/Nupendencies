using TomLonghurst.Nupendencies.Abstractions.Models;

namespace TomLonghurst.Nupendencies.Contracts;

public interface IIssuerRaiserService
{
    Task CreateIssues(UpdateReport updateReport, GitRepository gitRepository);
}