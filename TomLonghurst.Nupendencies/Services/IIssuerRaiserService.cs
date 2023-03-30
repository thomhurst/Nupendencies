using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Services;

public interface IIssuerRaiserService
{
    Task CreateIssues(UpdateReport updateReport, GitRepository gitRepository);
}