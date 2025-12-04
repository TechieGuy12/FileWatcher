# .NET 8.0 Upgrade Report

## Project target framework modifications

| Project name                      | Old Target Framework | New Target Framework | Commits         |
|:----------------------------------|:--------------------:|:--------------------:|:----------------|
| src\FileWatcher.csproj            | net6.0               | net8.0               | dd610f7c        |
| tests\FileWatcher.Tests.csproj    | net6.0               | net8.0               | 7a8d08e4        |

## NuGet Packages

| Package Name                                   | Old Version | New Version | Commit ID   |
|:-----------------------------------------------|:-----------:|:-----------:|:------------|
| Microsoft.Extensions.Hosting                   | 7.0.1       | 8.0.1       | 888e4f6c    |
| Microsoft.Extensions.Hosting.WindowsServices   | 7.0.1       | 8.0.1       | 888e4f6c    |
| Microsoft.Extensions.Http                      | 7.0.0       | 8.0.1       | 888e4f6c    |

## All commits

| Commit ID  | Description                                                                                                     |
|:-----------|:----------------------------------------------------------------------------------------------------------------|
| 45812efd   | Commit upgrade plan                                                                                             |
| dd610f7c   | Update FileWatcher.csproj to target .NET 8.0                                                                    |
| 888e4f6c   | Update package versions in FileWatcher.csproj                                                                   |
| 7a8d08e4   | Update FileWatcher.Tests.csproj to target .NET 8.0                                                              |

## Test Results

All unit tests passed successfully:
- **FileWatcher.Tests**: 44 passed, 0 failed, 0 skipped

## Next steps

### Self-contained deployment optimization

To create a small self-contained executable, consider adding these properties to `src\FileWatcher.csproj`:

#### Option 1: Trimmed + Single File (Recommended)
```xml
<PropertyGroup>
  <PublishTrimmed>true</PublishTrimmed>
  <PublishSingleFile>true</PublishSingleFile>
  <PublishReadyToRun>false</PublishReadyToRun>
  <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
</PropertyGroup>
```
Expected size reduction: 50-70%

#### Option 2: Native AOT (Smallest size, fastest startup)
```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
</PropertyGroup>
```
Expected size reduction: 70-90%, but has some limitations with reflection

#### Publish command:
```bash
dotnet publish -c Release -r win-x64 --self-contained
```
