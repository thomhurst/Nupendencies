@echo off
set nugetApiKey=
set /p version="Enter Version Number to Build With: "

@echo on

FOR /F "tokens=*" %%G IN ('DIR /B /AD /S bin') DO RMDIR /S /Q "%%G"
FOR /F "tokens=*" %%G IN ('DIR /B /AD /S obj') DO RMDIR /S /Q "%%G"

dotnet pack ".\TomLonghurst.Nupendencies.NetSdkLocator.Models\TomLonghurst.Nupendencies.NetSdkLocator.Models.csproj"  --configuration Release /p:Version=%version% 

SET NupkgPath=.\TomLonghurst.Nupendencies.NetSdkLocator.Models\bin\Release\TomLonghurst.Nupendencies.NetSdkLocator.Models.%version%.nupkg  
dotnet nuget push %NupkgPath% --api-key %nugetApiKey% --source https://api.nuget.org/v3/index.json

dotnet pack ".\TomLonghurst.Nupendencies.NetSdkLocator\TomLonghurst.Nupendencies.NetSdkLocator.csproj"  --configuration Release /p:Version=%version%

SET NupkgPath=.\TomLonghurst.Nupendencies.NetSdkLocator\bin\Release\TomLonghurst.Nupendencies.NetSdkLocator.%version%.nupkg  
dotnet nuget push %NupkgPath% --api-key %nugetApiKey% --source https://api.nuget.org/v3/index.json

dotnet pack ".\TomLonghurst.Nupendencies.Abstractions\TomLonghurst.Nupendencies.Abstractions.csproj"  --configuration Release /p:Version=%version%

SET NupkgPath=.\TomLonghurst.Nupendencies.Abstractions\bin\Release\TomLonghurst.Nupendencies.Abstractions.%version%.nupkg  
dotnet nuget push %NupkgPath% --api-key %nugetApiKey% --source https://api.nuget.org/v3/index.json

dotnet pack ".\TomLonghurst.Nupendencies\TomLonghurst.Nupendencies.csproj"  --configuration Release /p:Version=%version% 

SET NupkgPath=.\TomLonghurst.Nupendencies\bin\Release\TomLonghurst.Nupendencies.%version%.nupkg  
dotnet nuget push %NupkgPath% --api-key %nugetApiKey% --source https://api.nuget.org/v3/index.json

dotnet pack ".\TomLonghurst.Nupendencies.GitProviders.AzureDevOps\TomLonghurst.Nupendencies.GitProviders.AzureDevOps.csproj"  --configuration Release /p:Version=%version%

SET NupkgPath=.\TomLonghurst.Nupendencies.GitProviders.AzureDevOps\bin\Release\TomLonghurst.Nupendencies.GitProviders.AzureDevOps.%version%.nupkg  
dotnet nuget push %NupkgPath% --api-key %nugetApiKey% --source https://api.nuget.org/v3/index.json

dotnet pack ".\TomLonghurst.Nupendencies.GitProviders.GitHub\TomLonghurst.Nupendencies.GitProviders.GitHub.csproj" --configuration Release /p:Version=%version%

SET NupkgPath=.\TomLonghurst.Nupendencies.GitProviders.GitHub\bin\Release\TomLonghurst.Nupendencies.GitProviders.GitHub.%version%.nupkg  
dotnet nuget push %NupkgPath% --api-key %nugetApiKey% --source https://api.nuget.org/v3/index.json
pause