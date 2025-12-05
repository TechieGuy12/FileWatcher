# Testing Strategy

## Overview
This document outlines the testing strategy for FileWatcher, including current coverage, areas of focus, and guidelines for adding new tests.

## Current Test Coverage

### Coverage Status
- **Current Coverage**: ~9% (baseline after initial test suite)
- **Target Coverage**: 60%+ for critical paths
- **CI/CD Thresholds**: 
  - Warning: 20% (yellow indicator)
  - Failure: 40% (red indicator)

### What's Currently Tested

#### Configuration Tests
- **TriggersTests**: Trigger type handling and flag combinations
- **VariablesTests**: Variable management, merging, and case-insensitivity
- **FiltersTests**: File filtering configuration
- **ExclusionsTests**: File exclusion patterns
- **ChangeInfoTests**: Change event information tracking
- **DataTests**: HTTP request data configuration
- **CommandTests**: Command configuration and disposal
- **ActionsTests**: Basic action execution (partial)
- **NotificationsTests**: Notification system (basic)

#### FileSystem Tests
- **FileTests**: File name extraction with/without extensions
- **PatternMatcherTests**: Wildcard pattern matching for file filtering

#### Core Tests
- **PlaceholderTests**: Placeholder replacement in strings (paths, environment variables, custom variables)

#### IO Tests
- **NameTests**: Pattern matching for files and folders

## Critical Areas to Test

### High Priority (Core Functionality)
1. **Watch Class** (`src/Configuration/Watch.cs`)
   - File system watcher initialization
   - Queue processing
   - Change detection
   - Variable propagation to child elements

2. **Command Execution** (`src/Configuration/Command.cs`)
   - Process execution
   - Argument replacement
   - Working directory handling
   - Error handling

3. **Action Processing** (`src/Configuration/Action.cs`)
   - HTTP request construction
   - Request/response handling
   - Retry logic

4. **XmlFile Configuration** (`src/Configuration/XmlFile.cs`)
   - XML deserialization
   - Validation
   - Error handling

### Medium Priority (Supporting Functionality)
1. **Filters & Exclusions** (expand existing tests)
   - Pattern matching integration
   - Attribute filtering
   - Path-based filtering

2. **Workflows** (`src/Configuration/Workflows.cs`)
   - Step execution order
   - Conditional execution

3. **Placeholder Expansion** (expand existing tests)
   - More complex scenarios
   - Edge cases

### Lower Priority
1. **Logging** (`src/Log/Logger.cs`)
2. **File/Folder helpers** (already partially tested)

## Testing Guidelines

### Test Structure
All tests follow the Arrange-Act-Assert pattern:

```csharp
[Fact]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange
    var objectUnderTest = new ClassToTest();
    
    // Act
    var result = objectUnderTest.MethodToTest();
    
    // Assert
    result.Should().Be(expectedValue);
}
```

### Naming Conventions
- Test methods: `MethodName_Scenario_ExpectedBehavior`
- Test classes: `ClassNameTests`
- Test files: Located in `tests/` directory matching source structure

### Common Patterns

#### Testing Exceptions
```csharp
[Fact]
public void Method_WithInvalidInput_ShouldThrowException()
{
    System.Action act = () => objectUnderTest.Method(null);
    act.Should().Throw<ArgumentNullException>();
}
```

#### Testing with Theory (Multiple Test Cases)
```csharp
[Theory]
[InlineData("input1", "expected1")]
[InlineData("input2", "expected2")]
public void Method_WithVariousInputs_ShouldReturnExpected(string input, string expected)
{
    var result = objectUnderTest.Method(input);
    result.Should().Be(expected);
}
```

#### Cross-Platform Considerations
- **Always** use platform-agnostic paths when possible
- **Never** rely on OS-specific environment variables (use test-controlled variables)
- Use `Path.Combine()` instead of string concatenation
- Tests run on Ubuntu in CI/CD

### File System Tests
When testing file system operations:
- Create temporary directories in `Path.GetTempPath()`
- Use unique GUIDs for test directories to avoid conflicts
- Implement `IDisposable` for cleanup
- Handle cleanup failures gracefully (try-catch)

Example:
```csharp
public class MyTests : IDisposable
{
    private readonly string _testDirectory;
    
    public MyTests()
    {
        _testDirectory = Path.Combine(
            Path.GetTempPath(), 
            $"MyTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }
    
    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try { Directory.Delete(_testDirectory, true); }
            catch { }
        }
    }
}
```

### Avoiding Common Issues

#### Type Name Conflicts
Use fully qualified names when there are conflicts:
```csharp
// Bad: Ambiguous between System.Action and TE.FileWatcher.Configuration.Action
Action act = () => method();

// Good: Fully qualified
System.Action act = () => method();
```

#### Collection Types
Be aware of property types in the codebase:
```csharp
// Many collections in FileWatcher use HashSet, not Collection
Files = new Files
{
    Name = new HashSet<Name>  // Not Collection<Name>
    {
        new Name { Pattern = "*.txt" }
    }
}
```

## Running Tests

### Locally
```bash
# Run all tests
dotnet test tests/FileWatcher.Tests.csproj

# Run with coverage
dotnet test tests/FileWatcher.Tests.csproj --collect:"XPlat Code Coverage"

# Run specific test
dotnet test --filter "FullyQualifiedName=FileWatcher.Tests.PlaceholderTests.ReplacePlaceholders_WithEnvironmentVariable_ShouldReplace"
```

### CI/CD
Tests run automatically on:
- Push to `main`, `develop`, or `developv2` branches
- Pull requests to these branches

Coverage reports are:
- Generated as artifacts
- Posted as PR comments
- Used to enforce minimum thresholds

## Improving Coverage

### Quick Wins
1. Add tests for simple property getters/setters
2. Test constructor initialization
3. Test `Dispose()` methods
4. Test null/empty input validation

### Progressive Strategy
1. **Phase 1** (Current): Test core utilities and configuration classes ? Target: 20%
2. **Phase 2**: Add Watch and Command execution tests ? Target: 40%
3. **Phase 3**: Add Action and Workflow tests ? Target: 60%
4. **Phase 4**: Edge cases and error handling ? Target: 75%+

### Updating Thresholds
As coverage improves, update `.github/workflows/ci-cd.yml`:

```yaml
thresholds: '20 40'  # Current: warn at 20%, fail below 40%
# Update to:
thresholds: '40 60'  # Future: warn at 40%, fail below 60%
```

## Testing Tools

### Frameworks & Libraries
- **xUnit**: Test framework
- **FluentAssertions**: Assertion library for readable tests
- **Coverlet**: Code coverage tool

### CI/CD Integration
- **irongut/CodeCoverageSummary**: GitHub Action for coverage reports
- **marocchino/sticky-pull-request-comment**: PR comment integration
- **dorny/test-reporter**: Test result visualization

## Resources
- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [.NET Testing Best Practices](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
