using System.Collections.Concurrent;
using System.Text.Json;
using TomLonghurst.Nupendencies.Abstractions.Models;
using TomLonghurst.Nupendencies.Contracts;

namespace TomLonghurst.Nupendencies;

public class PreviousResultsService : IPreviousResultsService, IAsyncDisposable
{
    private readonly ConcurrentBag<PreviousProblem> _previousResults;
    private bool _shouldSave;
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    public PreviousResultsService()
    {
        if (!File.Exists(GetPath()))
        {
            _previousResults = new ConcurrentBag<PreviousProblem>();
        }
        else
        {
            try
            {
                var fileContents = JsonSerializer.Deserialize<List<PreviousProblem>>(File.ReadAllText(GetPath()));
            
                _previousResults = new ConcurrentBag<PreviousProblem>(fileContents
                    .Where(x => x.DateTimeOffset > DateTimeOffset.UtcNow - TimeSpan.FromDays(30))
                    .ToList());
            }
            catch
            {
                _previousResults = new ConcurrentBag<PreviousProblem>();
            }
        }

        Task.Factory.StartNew(SavePeriodically, TaskCreationOptions.LongRunning);
    }

    public void WriteUnableToRemovePackageEntry(ProjectPackage package)
    {
        _previousResults.Add(new PreviousProblem(DateTimeOffset.UtcNow, package.Name, package.Project.ProjectPath, PreviousProblemReason.UnableToRemoveDependency));
        _shouldSave = true;
    }

    public bool ShouldTryRemove(ProjectPackage package)
    {
        return !_previousResults.Any(x => x.PackageName == package.Name
                                          && Path.GetFileName(x.Project) == Path.GetFileName(package.Project.ProjectPath)
                                          && x.PreviousProblemReason == PreviousProblemReason.UnableToRemoveDependency
        );
    }

    public async ValueTask DisposeAsync()
    {
        await Save();
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }

    private string GetPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "Nupendencies", "PreviousResultsRemovals.json");
    }

    private async Task SavePeriodically()
    {
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(1));
            await Save();
        }
    }

    private async Task Save()
    {
        if (!_shouldSave)
        {
            return;
        }
        
        try
        {
            var path = GetPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(_previousResults));
            _shouldSave = false;
        }
        catch
        {
        }
    }
}