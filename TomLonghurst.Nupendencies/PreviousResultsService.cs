using System.Collections.Concurrent;
using System.Text.Json;

namespace TomLonghurst.Nupendencies;

public class PreviousResultsService : IPreviousResultsService, IAsyncDisposable
{
    private readonly ConcurrentBag<PreviousProblem> _previousResults;
    private bool _shouldSave;

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

    public void WriteUnableToRemovePackageEntry(string packageName, string project)
    {
        _previousResults.Add(new PreviousProblem(DateTimeOffset.UtcNow, packageName, project, PreviousProblemReason.UnableToRemoveDependency));
        _shouldSave = true;
    }

    public bool ShouldTryRemove(string packageName, string project)
    {
        return !_previousResults.Any(x => x.PackageName == packageName
                                          && Path.GetFileName(x.Project) == Path.GetFileName(project)
                                          && x.PreviousProblemReason == PreviousProblemReason.UnableToRemoveDependency
        );
    }

    public async ValueTask DisposeAsync()
    {
        await Save();
    }

    private string GetPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "Nupendencies", "PreviousResultsRemovals.json");
    }

    private async Task SavePeriodically()
    {
        while (true)
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