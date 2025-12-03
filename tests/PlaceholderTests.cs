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
            var envVarName = "TEMP";
            var expectedValue = Environment.GetEnvironmentVariable(envVarName);
            var value = $"Temp: [env:{envVarName}]";

            // Act
            var result = _placeholder.ReplacePlaceholders(value, _watchPath, testFile, null, null);

            // Assert
            result.Should().Be($"Temp: {expectedValue}");
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
    }
}
