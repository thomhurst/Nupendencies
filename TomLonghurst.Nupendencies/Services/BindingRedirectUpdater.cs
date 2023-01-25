using System.Xml.Linq;
using System.Xml.XPath;
using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Services;

public class BindingRedirectUpdater
{
    private static async Task UpdateBindingRedirects(string solutionPath, List<PackageUpdateResult> packageVersionInformations)
    {
        var directory = Path.GetDirectoryName(solutionPath);

        var webConfigFilePaths =  Directory.GetFiles(directory, "*.config", SearchOption.AllDirectories)
            .Where(x => x.EndsWith("web.config", StringComparison.OrdinalIgnoreCase))
            .Where(x => File.ReadAllText(x).Contains("<bindingRedirect"))
            .ToList();
        
        foreach (var webConfigFilePath in webConfigFilePaths)
        {
            var webConfigFile = XDocument.Parse(File.ReadAllText(webConfigFilePath));

            var dependentAssemblies = webConfigFile.Root
                .XPathSelectElements("//*[local-name()='dependentAssembly']")
                .ToList();

            foreach (var dependentAssembly in dependentAssemblies)
            {
                var packageVersionInformation = packageVersionInformations.FirstOrDefault(x =>
                {
                    var dependentAssemblyPackageName = dependentAssembly.XPathSelectElement("*[local-name()='assemblyIdentity']").Attribute("name").Value;
                    return x.PackageName == dependentAssemblyPackageName;
                });

                if (packageVersionInformation?.UpdateBuiltSuccessfully == true)
                {
                    var bindingRedirectElement = dependentAssembly.XPathSelectElement("*[local-name()='bindingRedirect']");
                    bindingRedirectElement.SetAttributeValue("oldVersion", $"0.0.0.0-{packageVersionInformation.GetBindingRedirectVersionString()}");
                    bindingRedirectElement.SetAttributeValue("newVersion", packageVersionInformation.GetBindingRedirectVersionString());
                }
            }

            webConfigFile.Save(webConfigFilePath);
        }
    }
    
    private async Task ReplaceLine(string filePath, int lineNumber, string value)
    {
        var lines = File.ReadAllLines(filePath);
        
        lines[lineNumber - 1] = value;
        
        File.WriteAllLines(filePath, lines);
    }
}