using TomLonghurst.Nupendencies.Models;
using TomLonghurst.Nupendencies.Services;

namespace TomLonghurst.Nupendencies.Contracts;

public interface IIssuerRaiserService
{
    Task CreateIssues(UpdateReport updateReport, GitRepository gitRepository);
}