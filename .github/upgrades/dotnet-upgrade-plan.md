# .NET 8.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that a .NET 8.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 8.0 upgrade.
3. Upgrade src\FileWatcher.csproj
4. Upgrade tests\FileWatcher.Tests.csproj
5. Run unit tests to validate upgrade in the projects listed below:
   - tests\FileWatcher.Tests.csproj

## Settings

This section contains settings and data used by execution steps.

### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name                                   | Current Version | New Version | Description                               |
|:-----------------------------------------------|:---------------:|:-----------:|:------------------------------------------|
| Microsoft.Extensions.Hosting                   | 7.0.1           | 8.0.1       | Recommended for .NET 8.0                  |
| Microsoft.Extensions.Hosting.WindowsServices   | 7.0.1           | 8.0.1       | Recommended for .NET 8.0                  |
| Microsoft.Extensions.Http                      | 7.0.0           | 8.0.1       | Recommended for .NET 8.0                  |

### Project upgrade details

This section contains details about each project upgrade and modifications that need to be done in the project.

#### src\FileWatcher.csproj modifications

Project properties changes:
  - Target framework should be changed from `net6.0` to `net8.0`

NuGet packages changes:
  - Microsoft.Extensions.Hosting should be updated from `7.0.1` to `8.0.1` (*recommended for .NET 8.0*)
  - Microsoft.Extensions.Hosting.WindowsServices should be updated from `7.0.1` to `8.0.1` (*recommended for .NET 8.0*)
  - Microsoft.Extensions.Http should be updated from `7.0.0` to `8.0.1` (*recommended for .NET 8.0*)

#### tests\FileWatcher.Tests.csproj modifications

Project properties changes:
  - Target framework should be changed from `net6.0` to `net8.0`
