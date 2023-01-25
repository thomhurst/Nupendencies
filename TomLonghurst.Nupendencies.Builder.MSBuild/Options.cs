using CommandLine;

namespace TomLonghurst.Nupendencies.Builder.MSBuild
{
    public class Options
    {
        [Option("azureArtifactsCredentialsJson", Required = false, HelpText = "A json array of Azure Artifacts endpoints, usernames and passwords")]
        public string AzureArtifactsCredentialsJson { get; set; }

        [Option('p', "project", Required = false, HelpText = "Absolute path to project file")]
        public string ProjectFilePath { get; set; }
    }
}