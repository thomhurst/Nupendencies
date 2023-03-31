using TomLonghurst.Nupendencies.Abstractions.Models;
using GitRepository = TomLonghurst.Nupendencies.Models.GitRepository;

namespace TomLonghurst.Nupendencies.Contracts;

public interface IIssuerRaiserService
{
    Task CreateIssues(UpdateReport updateReport, GitRepository gitRepository);
}