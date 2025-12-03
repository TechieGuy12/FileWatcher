# GitHub Actions Workflows

This repository includes automated CI/CD workflows using GitHub Actions.

## Workflows

### 1. `.github/workflows/tests.yml` - Basic Test Workflow
**Triggers:** Push and Pull Requests to `main`, `develop`, and `developv2` branches

**Steps:**
- ? Checkout code
- ? Setup .NET 6
- ? Restore dependencies (main project and tests)
- ? Build main project
- ? Build test project
- ? Run all tests
- ? Upload test results as artifacts
- ? Generate test summary report

**Note:** This workflow builds only the FileWatcher and FileWatcher.Tests projects, excluding the WiX installer project to avoid build complications in CI/CD.

**Usage:**
```bash
# Runs automatically on push/PR
# View results in the Actions tab
```

### 2. `.github/workflows/ci-cd.yml` - Full CI/CD Pipeline
**Triggers:** Push and Pull Requests to `main`, `develop`, and `developv2` branches

**Steps:**
- ? Checkout code with full history
- ? Setup .NET 6
- ? Cache NuGet packages for faster builds
- ? Restore dependencies (main project and tests)
- ? Build main project in Release mode
- ? Build test project in Release mode
- ? Run tests with code coverage
- ? Upload test results and coverage reports
- ? Generate code coverage summary
- ? Add coverage report as PR comment
- ? Generate test report
- ? Run code quality analysis (on push to main branches)

**Note:** This workflow builds only the FileWatcher and FileWatcher.Tests projects, excluding the WiX installer project. The installer should be built separately when creating releases.

**Code Coverage Thresholds:**
- ?? Warning: < 60%
- ?? Good: 60-80%
- ?? Excellent: > 80%

## Status Badges

Add these badges to your main README.md:

```markdown
![Tests](https://github.com/TechieGuy12/FileWatcher/workflows/.NET%20Tests/badge.svg?branch=developv2)
![CI/CD](https://github.com/TechieGuy12/FileWatcher/workflows/CI/CD%20Pipeline/badge.svg?branch=developv2)
```

## Viewing Results

### Test Results
1. Go to the **Actions** tab in GitHub
2. Click on the latest workflow run
3. View the **Test Results** section
4. Download artifacts for detailed reports

### Code Coverage
1. View coverage summary in workflow logs
2. Download coverage reports from artifacts
3. PR comments will show coverage changes

### Test Reports
- Automatic test reports are generated for each run
- Failed tests are highlighted with details
- Test duration and status for each test

## Local Testing

Run the same tests locally:

```bash
# Basic tests
dotnet test tests/FileWatcher.Tests.csproj

# With coverage
dotnet test tests/FileWatcher.Tests.csproj --collect:"XPlat Code Coverage"

# With detailed output
dotnet test tests/FileWatcher.Tests.csproj --verbosity detailed

# Build in Release mode (like CI)
dotnet build src/FileWatcher.csproj --configuration Release
dotnet build tests/FileWatcher.Tests.csproj --configuration Release
dotnet test tests/FileWatcher.Tests.csproj --configuration Release --no-build
```

## Building the Full Solution

To build the complete solution including the WiX installer:

```bash
# Build the entire solution (including WiX installer)
dotnet build FileWatcher.sln --configuration Release

# Note: WiX installer requires WiX Toolset to be installed
# Download from: https://wixtoolset.org/
```

## Artifacts

The workflows generate the following artifacts (retained for 30 days):

- **test-results**: TRX files with detailed test results
- **coverage-reports**: Cobertura XML coverage reports

Download from the workflow run page in GitHub Actions.

## Troubleshooting

### Tests failing in CI but passing locally?
- Ensure you're building in Release mode: `dotnet build -c Release`
- Check for file path differences (Windows vs Linux)
- Review the full logs in the Actions tab

### Coverage not generating?
- Ensure coverlet.collector package is installed in test project
- Check that tests are actually running
- Verify `--collect:"XPlat Code Coverage"` parameter

### Workflow not triggering?
- Check that `.github/workflows/` directory is in the root
- Verify branch names match in `on:` section
- Ensure YAML syntax is valid

### WiX Installer Build Issues?
- The CI/CD workflows intentionally exclude the WiX installer project
- WiX builds require special setup and are typically done during release creation
- To build the installer locally, ensure WiX Toolset 4.0+ is installed

## Future Enhancements

Potential additions to the CI/CD pipeline:

- [ ] SonarCloud integration for code quality
- [ ] Automated release creation on version tags
- [ ] Deploy to Azure/AWS on successful builds
- [ ] Performance benchmarking
- [ ] Security vulnerability scanning
- [ ] Docker image building and publishing
- [ ] Separate workflow for building WiX installer on releases
