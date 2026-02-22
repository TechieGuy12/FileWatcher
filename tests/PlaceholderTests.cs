using FluentAssertions;
using System.Collections.Concurrent;
using TE.FileWatcher;
using Xunit;
using IODirectory = System.IO.Directory;

namespace FileWatcher.Tests
{
    public class PlaceholderTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly string _watchPath;
        private readonly List<string> _testFiles = new();
        private readonly Placeholder _placeholder;

        public PlaceholderTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), $"PlaceholderTests_{Guid.NewGuid()}");
            IODirectory.CreateDirectory(_testDirectory);
            _watchPath = _testDirectory;
            _placeholder = new Placeholder();
        }

        public void Dispose()
        {
            foreach (var file in _testFiles.Where(File.Exists))
            {
                try
                {
                    File.Delete(file);
                }
                catch { }
            }

            if (IODirectory.Exists(_testDirectory))
            {
                try
                {
                    IODirectory.Delete(_testDirectory, true);
                }
                catch { }
            }
        }

        private string CreateTestFile(string filename)
        {
            var path = Path.Combine(_testDirectory, filename);
            File.WriteAllText(path, "test content");
            _testFiles.Add(path);
            return path;
        }

        [Fact]
        public void ReplacePlaceholders_WatchPath_ShouldReplaceCorrectly()
        {
            // Arrange
            var testFile = CreateTestFile("test.txt");
            var value = "Path: [watchpath]";

            // Act
            var result = _placeholder.ReplacePlaceholders(value, _watchPath, testFile, null, null);

            // Assert
            result.Should().Be($"Path: {_watchPath}");
        }

        [Fact]
        public void ReplacePlaceholders_FullPath_ShouldReplaceWithRelativePath()
        {
            // Arrange
            var subDir = Path.Combine(_testDirectory, "subdir");
            IODirectory.CreateDirectory(subDir);
            var testFile = Path.Combine(subDir, "test.txt");
            File.WriteAllText(testFile, "content");
            _testFiles.Add(testFile);
            
            var value = "File: [fullpath]";

            // Act
            var result = _placeholder.ReplacePlaceholders(value, _watchPath, testFile, null, null);

            // Assert
            result.Should().Be($"File: subdir{Path.DirectorySeparatorChar}test.txt");
        }

        [Fact]
        public void ReplacePlaceholders_FileName_ShouldReplaceWithFilenameAndExtension()
        {
            // Arrange
            var testFile = CreateTestFile("document.pdf");
            var value = "Name: [filename]";

            // Act
            var result = _placeholder.ReplacePlaceholders(value, _watchPath, testFile, null, null);

            // Assert
            result.Should().Be("Name: document.pdf");
        }

        [Fact]
        public void ReplacePlaceholders_File_ShouldReplaceWithFilenameWithoutExtension()
        {
            // Arrange
            var testFile = CreateTestFile("document.pdf");
            var value = "Name: [file]";

            // Act
            var result = _placeholder.ReplacePlaceholders(value, _watchPath, testFile, null, null);

            // Assert
            result.Should().Be("Name: document");
        }

        [Fact]
        public void ReplacePlaceholders_Extension_ShouldReplaceWithExtension()
        {
            // Arrange
            var testFile = CreateTestFile("document.pdf");
            var value = "Ext: [extension]";

            // Act
            var result = _placeholder.ReplacePlaceholders(value, _watchPath, testFile, null, null);

            // Assert
            result.Should().Be("Ext: .pdf");
        }

        [Fact]
        public void ReplacePlaceholders_MultiplePlaceholders_ShouldReplaceAll()
        {
            // Arrange
            var testFile = CreateTestFile("report.txt");
            var value = "[watchpath]\\[filename] -> [file][extension]";

            // Act
            var result = _placeholder.ReplacePlaceholders(value, _watchPath, testFile, null, null);

            // Assert
            result.Should().Be($"{_watchPath}\\report.txt -> report.txt");
        }

        [Fact]
        public void ReplacePlaceholders_WithEnvironmentVariable_ShouldReplace()
        {
            // Arrange
            var testFile = CreateTestFile("test.txt");
            var envVarName = "FILEWATCHER_TEST_VAR";
            var envVarValue = "TestEnvironmentValue";
            Environment.SetEnvironmentVariable(envVarName, envVarValue);
            
            try
            {
                var value = $"Temp: [env:{envVarName}]";

                // Act
                var result = _placeholder.ReplacePlaceholders(value, _watchPath, testFile, null, null);

                // Assert
                result.Should().Be($"Temp: {envVarValue}");
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable(envVarName, null);
            }
        }

        [Fact]
        public void ReplacePlaceholders_WithCustomVariable_ShouldReplace()
        {
            // Arrange
            var testFile = CreateTestFile("test.txt");
            var variables = new ConcurrentDictionary<string, string>();
            variables.TryAdd("customVar", "CustomValue");
            var value = "Value: [var:customVar]";

            // Act
            var result = _placeholder.ReplacePlaceholders(value, _watchPath, testFile, null, variables);

            // Assert
            result.Should().Be("Value: CustomValue");
        }

        [Fact]
        public void ReplacePlaceholders_WithUrlEncoding_ShouldEncode()
        {
            // Arrange
            var testFile = CreateTestFile("test.txt");
            var value = "[urlenc:Hello World & Stuff]";

            // Act
            var result = _placeholder.ReplacePlaceholders(value, _watchPath, testFile, null, null);

            // Assert
            result.Should().Be("Hello+World+%26+Stuff");
        }

        [Fact]
        public void ReplacePlaceholders_NullValue_ShouldReturnNull()
        {
            // Arrange
            var testFile = CreateTestFile("test.txt");

            // Act
            var result = _placeholder.ReplacePlaceholders(null!, _watchPath, testFile, null, null);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ReplacePlaceholders_EmptyValue_ShouldReturnNull()
        {
            // Arrange
            var testFile = CreateTestFile("test.txt");

            // Act
            var result = _placeholder.ReplacePlaceholders(string.Empty, _watchPath, testFile, null, null);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ReplacePlaceholders_CaseInsensitive_ShouldReplace()
        {
            // Arrange
            var testFile = CreateTestFile("test.txt");
            var value = "[FILENAME] [FileName] [filename]";

            // Act
            var result = _placeholder.ReplacePlaceholders(value, _watchPath, testFile, null, null);

            // Assert
            result.Should().Be("test.txt test.txt test.txt");
        }

        [Fact]
        public void ReplacePlaceholders_Path_ShouldReplaceWithDirectoryPath()
        {
            // Arrange
            var subdirPath = Path.Combine(_testDirectory, "subdir");
            IODirectory.CreateDirectory(subdirPath);
            var testFile = Path.Combine(subdirPath, "file.txt");
            File.WriteAllText(testFile, "content");
            _testFiles.Add(testFile);
            var value = "Path: [path]";

            // Act
            var result = _placeholder.ReplacePlaceholders(value, _watchPath, testFile, null, null);

            // Assert
            result.Should().Contain("subdir");
        }

        [Fact]
        public void ReplacePlaceholders_WatchPath_ShouldReplaceWithWatchPath()
        {
            // Arrange
            var testFile = CreateTestFile("file.txt");
            var value = "Watch: [watchpath]";

            // Act
            var result = _placeholder.ReplacePlaceholders(value, _watchPath, testFile, null, null);

            // Assert
            result.Should().Be($"Watch: {_watchPath}");
        }

        [Fact]
        public void ReplacePlaceholders_WithNullOldPath_ShouldNotReplaceOldPlaceholders()
        {
            // Arrange
            var testFile = CreateTestFile("file.txt");
            var value = "Old: [oldfile]";

            // Act
            var result = _placeholder.ReplacePlaceholders(value, _watchPath, testFile, null, null);

            // Assert - oldfile placeholder remains when oldPath is null
            result.Should().Be("Old: [oldfile]");
        }

        [Fact]
        public void ReplacePlaceholders_MixedPlaceholders_ShouldReplaceAll()
        {
            // Arrange
            var testFile = CreateTestFile("test.txt");
            var value = "[filename]";

            // Act
            var result = _placeholder.ReplacePlaceholders(value, _watchPath, testFile, null, null);

            // Assert
            result.Should().Be("test.txt");
        }

        [Fact]
        public void ReplacePlaceholders_EmptyOldPath_ShouldHandleGracefully()
        {
            // Arrange
            var testFile = CreateTestFile("file.txt");
            var value = "[oldfile]";

            // Act
            var result = _placeholder.ReplacePlaceholders(value, _watchPath, testFile, "", null);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public void ReplacePlaceholders_EnvironmentVariable_ShouldCache()
        {
            // Arrange
            var testEnvVar = "TEST_PLACEHOLDER_ENV";
            var testEnvValue = "TestValue123";
            Environment.SetEnvironmentVariable(testEnvVar, testEnvValue);
            
            try
            {
                var testFile = CreateTestFile("test.txt");
                var value = $"Value: [env:{testEnvVar}]";
                
                // Clear cache before test
                Placeholder.ClearCache();
                
                // Act - First call (cache miss)
                var result1 = _placeholder.ReplacePlaceholders(value, _watchPath, testFile, null, null);
                var hitRateAfterFirst = Placeholder.CacheHitRate;
                
                // Act - Second call (cache hit)
                var result2 = _placeholder.ReplacePlaceholders(value, _watchPath, testFile, null, null);
                var hitRateAfterSecond = Placeholder.CacheHitRate;

                // Assert
                result1.Should().Be($"Value: {testEnvValue}");
                result2.Should().Be($"Value: {testEnvValue}");
                hitRateAfterFirst.Should().Be(0.0); // First call is a miss
                hitRateAfterSecond.Should().BeGreaterThan(0.0); // Second call should have hits
            }
            finally
            {
                Environment.SetEnvironmentVariable(testEnvVar, null);
                Placeholder.ClearCache();
            }
        }

        [Fact]
        public void ReplacePlaceholders_Variable_ShouldCache()
        {
            // Arrange
            var testFile = CreateTestFile("test.txt");
            var variables = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            variables.TryAdd("myvar", "myvalue");
            var value = "Var: [var:myvar]";
            
            // Clear cache before test
            Placeholder.ClearCache();
            
            // Act - First call (cache miss)
            var result1 = _placeholder.ReplacePlaceholders(value, _watchPath, testFile, null, variables);
            var hitRateAfterFirst = Placeholder.CacheHitRate;
            
            // Act - Second call (cache hit)
            var result2 = _placeholder.ReplacePlaceholders(value, _watchPath, testFile, null, variables);
            var hitRateAfterSecond = Placeholder.CacheHitRate;

            // Assert
            result1.Should().Be("Var: myvalue");
            result2.Should().Be("Var: myvalue");
            hitRateAfterFirst.Should().Be(0.0); // First call is a miss
            hitRateAfterSecond.Should().BeGreaterThan(0.0); // Second call should have hits
            
            Placeholder.ClearCache();
        }

        [Fact]
        public void ReplacePlaceholders_UrlEncode_ShouldCache()
        {
            // Arrange
            var testFile = CreateTestFile("test.txt");
            var urlToEncode = "hello world & special chars!";
            var value = $"URL: [urlenc:{urlToEncode}]";
            
            // Clear cache before test
            Placeholder.ClearCache();
            
            // Act - First call (cache miss)
            var result1 = _placeholder.ReplacePlaceholders(value, _watchPath, testFile, null, null);
            var hitRateAfterFirst = Placeholder.CacheHitRate;
            
            // Act - Second call (cache hit)
            var result2 = _placeholder.ReplacePlaceholders(value, _watchPath, testFile, null, null);
            var hitRateAfterSecond = Placeholder.CacheHitRate;

            // Assert
            result1.Should().Contain("hello+world");
            result2.Should().Be(result1); // Same result
            hitRateAfterFirst.Should().Be(0.0); // First call is a miss
            hitRateAfterSecond.Should().BeGreaterThan(0.0); // Second call should have hits
            
            Placeholder.ClearCache();
        }

        [Fact]
        public void ClearCache_ShouldResetCacheStatistics()
        {
            // Arrange
            var testFile = CreateTestFile("test.txt");
            var testEnvVar = "TEST_CACHE_CLEAR";
            Environment.SetEnvironmentVariable(testEnvVar, "value");
            
            try
            {
                var value = $"[env:{testEnvVar}]";
                
                // Act - Build up cache
                _placeholder.ReplacePlaceholders(value, _watchPath, testFile, null, null);
                _placeholder.ReplacePlaceholders(value, _watchPath, testFile, null, null);
                
                var hitRateBeforeClear = Placeholder.CacheHitRate;
                
                // Clear cache
                Placeholder.ClearCache();
                
                var hitRateAfterClear = Placeholder.CacheHitRate;

                // Assert
                hitRateBeforeClear.Should().BeGreaterThan(0.0);
                hitRateAfterClear.Should().Be(0.0);
            }
            finally
            {
                Environment.SetEnvironmentVariable(testEnvVar, null);
                Placeholder.ClearCache();
            }
        }
    }
}
