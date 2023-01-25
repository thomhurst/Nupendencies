// using System.Diagnostics;
// using Microsoft.Extensions.Logging;
//
// namespace TomLonghurst.Nupendencies.Services;
//
// public class CommandExecutor : ICommandExecutor
// {
//     private readonly ILogger<CommandExecutor> _logger;
//
//     public CommandExecutor(ILogger<CommandExecutor> logger)
//     {
//         _logger = logger;
//     }
//     
//     public async Task RunCommand(string workingDirectory, string command)
//     {
//         var startInfo = new ProcessStartInfo
//         {
//             WorkingDirectory = workingDirectory,
//             FileName = "cmd.exe",
//             Arguments = $"/c {command}",
//             
//         };
//
//         var process = Process.Start(startInfo);
//         
//         await process.WaitForExitAsync();
//     }
//     
//     public async Task<PSDataCollection<PSObject>> RunPowershell(string workingDirectory, params string[] commands)
//     {
//         var powershell = PowerShell.Create();
//
//         powershell.AddScript($"Set-Location \"{workingDirectory}\"");
//         foreach (var command in commands)
//         {
//             powershell.AddScript(command);   
//         }
//
//         //powershell.AddScript("$LASTEXITCODE");
//
//         var psDataCollection = await powershell.InvokeAsync();
//         
//         foreach (var psObject in psDataCollection)
//         {
//             _logger.LogTrace("Powershell Output: {Output}", psObject.ToString());
//         }
//
//         return psDataCollection;
//     }
// }