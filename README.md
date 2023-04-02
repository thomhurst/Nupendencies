# Nupendencies
Automated NuGet Dependency Updater

## What does it do?
- It will scan your repositories in your Git Provider (Currently supporting Azure DevOps and GitHub)
- It will clone each repository to the local machine
- It will build a Project tree, and detect all TargetFramework + PackageReference elements within your csproj files
- It will attempt to update the TargetFramework (if you are below the latest known version by this tool) and then build your project, rolling back if it breaks compilation
- (Optional by configuration) It will attempt to delete ProjectReferences to de-clutter csproj files where they're redundant (because they're unused or pulled in from other projects), rolling back if it breaks compilation
- (Optional by configuration) It will attempt to delete PackageReferences to de-clutter csproj files where they're redundant (because they're unused or pulled in from other projects/packages), rolling back if it breaks compilation or detects an overall downgrade to that package throughout the project tree
- It will attempt to upgrade PackageReferences with the latest stable version from NuGet, rolling back if it breaks compilation
- It will raise issues (Currently only on GitHub) for any packages that it couldn't update automatically
- It will raise a Pull Request with the successful updates
- It will delete the cloned repository from the local machine

## Pre-Requisites
- .NET 7 required
- Your machine will need Git installed
- Your machine will need the .NET SDK (not just runtime) installed to build projects

Install via NuGet
- TomLonghurst.Nupendencies
- TomLonghurst.Nupendencies.GitProviders.AzureDevOps
- TomLonghurst.Nupendencies.GitProviders.GitHub

## How to set it up?
See the code below for an example on how to use it.

```csharp
var host = Host.CreateDefaultBuilder()
    .ConfigureAppConfiguration(cfg =>
    {
        cfg.AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .AddUserSecrets<Program>();
    })
    .ConfigureServices((context, services) =>
    {
        services
            .AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddSimpleConsole(console =>
                {
                    console.TimestampFormat = "[HH:mm:ss] ";
                });
            })
            .AddNupendencies(new NupendenciesOptions
            {
                CommitterEmail = "myemail@example.com",
                CommitterName = "Tom Longhurst",
                TryRemoveUnusedPackages = true,  // Remove redundant PackageReference tags - Redundant if code is unused or they're pulled in transitively from other projects
                TryRemoveUnusedProjects = true // Remove redundant ProjectReference tags - Redundant if code is unused or they're pulled in transitively from other projects
            })
            .AddGitHubProvider(new GitHubOptions
            {
                GitHubSpace = GitHubSpace.Team("organization-name", "github-team-name") // or GitHubSpace.User() for your regular GitHub user's repositories
                AuthenticationPatToken = "my-pat",
                AuthenticationUsername = "my-github-username"
            })
            .AddAzureDevOpsProvider(new AzureDevOpsOptions
            {
                Organization = "organization-name",
                ProjectName = "azure-devops-project-name",
                
                AuthenticationPatToken = "my-pat",
                AuthenticationUsername = "myemail@example.com",
                WorkItemIdsToAttachToPullRequests = new[] {
                "12345" // User Story to automatically attach to created PRs
                }
            });
    })
    .Build();
    
await host.Services.GetRequiredService<INupendencyUpdater>().Start();
```

## Private NuGet Feeds
Nupendencies supports private NuGet feeds, allowing you to still check for updates for private packages.
The NupendenciesOptions object can take an array of Private NuGet Feed options, where you provide a URL, a source nickname (as you would in your NuGet config file), your authentication username, your authentication PAT

```csharp
var nupendenciesOptions = new NupendenciesOptions()
        {
            // ...
            PrivateNugetFeedOptions =
            {
                new PrivateNugetFeedOptions()
                {
                    Username = "myemail@example.com",
                    PatToken = "mypat",
                    SourceName = "MyPrivateFeed",
                    SourceUrl = "https://example.com/_packaging/MyPrivateFeed/nuget/v3/index.json"
                }
            }
            // ...
        }
```

## Things to consider
Since this builds projects over and over to check packages don't break compilation, it can be slow to run, especially if you have a very large project with many package references / nested projects.
You may want to run this on a fast machine to decrease project build times
