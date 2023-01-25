namespace TomLonghurst.Nupendencies.Models
{
    public record NugetPackageVersionsResponse
    {
        public List<string> Versions { get; set; }
    }
}