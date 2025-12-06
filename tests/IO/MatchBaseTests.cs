using FluentAssertions;
using TE.FileWatcher.Configuration;
using TE.FileWatcher.IO;
using Xunit;
using IOFile = System.IO.File;
using IODirectory = System.IO.Directory;

namespace FileWatcher.Tests.IO
{
    // Concrete implementation for testing MatchBase
    public class TestMatch : MatchBase
    {
        public TestMatch()
        {
            FilterTypeName = "TestFilter";
        }

        public bool TestAttributeMatch(string path) => AttributeMatch(path);
        public bool TestFileMatch(string name) => FileMatch(name);
    }

    public class MatchBaseTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly List<string> _testFiles = new();

        public MatchBaseTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), $"MatchBaseTests_{Guid.NewGuid()}");
            IODirectory.CreateDirectory(_testDirectory);
        }

        public void Dispose()
        {
            foreach (var file in _testFiles.Where(IOFile.Exists))
            {
                try { IOFile.Delete(file); }
                catch { }
            }
            if (IODirectory.Exists(_testDirectory))
            {
                try { IODirectory.Delete(_testDirectory, true); }
                catch { }
            }
        }

        private string CreateTestFile(string filename)
        {
            var path = Path.Combine(_testDirectory, filename);
            IOFile.WriteAllText(path, "test");
            _testFiles.Add(path);
            return path;
        }

        [Fact]
        public void MatchBase_ShouldInitializeWithNullProperties()
        {
            // Act
            var match = new TestMatch();

            // Assert
            match.Files.Should().BeNull();
            match.Folders.Should().BeNull();
            match.Paths.Should().BeNull();
            match.Attributes.Should().BeNull();
        }

        [Fact]
        public void MatchBase_Log_ShouldDefaultToTrue()
        {
            // Act
            var match = new TestMatch();

            // Assert
            match.Log.Should().BeTrue();
        }

        [Fact]
        public void IsSpecified_WithNoFilters_ShouldReturnFalse()
        {
            // Arrange
            var match = new TestMatch();

            // Act & Assert
            match.IsSpecified().Should().BeFalse();
        }

        [Fact]
        public void IsSpecified_WithFiles_ShouldReturnTrue()
        {
            // Arrange
            var match = new TestMatch
            {
                Files = new Files()
            };
            match.Files.Name.Add(new Name { Pattern = "*.txt" });

            // Act & Assert
            match.IsSpecified().Should().BeTrue();
        }

        [Fact]
        public void IsSpecified_WithFolders_ShouldReturnTrue()
        {
            // Arrange
            var match = new TestMatch
            {
                Folders = new Folders()
            };
            match.Folders.Name.Add(new Name { Pattern = "temp" });

            // Act & Assert
            match.IsSpecified().Should().BeTrue();
        }

        [Fact]
        public void IsSpecified_WithPaths_ShouldReturnTrue()
        {
            // Arrange
            var match = new TestMatch
            {
                Paths = new Paths()
            };
            match.Paths.Path.Add("C:\\test");

            // Act & Assert
            match.IsSpecified().Should().BeTrue();
        }

        [Fact]
        public void IsSpecified_WithAttributes_ShouldReturnTrue()
        {
            // Arrange
            var match = new TestMatch
            {
                Attributes = new Attributes()
            };
            match.Attributes.AttributeStrings.Add("Hidden");

            // Act & Assert
            match.IsSpecified().Should().BeTrue();
        }

        [Fact]
        public void IsSpecified_WithEmptyFiles_ShouldReturnFalse()
        {
            // Arrange
            var match = new TestMatch
            {
                Files = new Files()
            };

            // Act & Assert
            match.IsSpecified().Should().BeFalse();
        }

        [Fact]
        public void IsSpecified_WithMultipleFilters_ShouldReturnTrue()
        {
            // Arrange
            var match = new TestMatch
            {
                Files = new Files(),
                Folders = new Folders()
            };
            match.Files.Name.Add(new Name { Pattern = "*.txt" });
            match.Folders.Name.Add(new Name { Pattern = "temp" });

            // Act & Assert
            match.IsSpecified().Should().BeTrue();
        }

        [Fact]
        public void FileMatch_WithNullFiles_ShouldReturnFalse()
        {
            // Arrange
            var match = new TestMatch();

            // Act & Assert
            match.TestFileMatch("test.txt").Should().BeFalse();
        }

        [Fact]
        public void FileMatch_WithEmptyFiles_ShouldReturnFalse()
        {
            // Arrange
            var match = new TestMatch
            {
                Files = new Files()
            };

            // Act & Assert
            match.TestFileMatch("test.txt").Should().BeFalse();
        }

        [Fact]
        public void FileMatch_WithNullName_ShouldReturnFalse()
        {
            // Arrange
            var match = new TestMatch
            {
                Files = new Files()
            };
            match.Files.Name.Add(new Name { Pattern = "*.txt" });

            // Act & Assert
            match.TestFileMatch(null!).Should().BeFalse();
        }

        [Fact]
        public void FileMatch_WithEmptyName_ShouldReturnFalse()
        {
            // Arrange
            var match = new TestMatch
            {
                Files = new Files()
            };
            match.Files.Name.Add(new Name { Pattern = "*.txt" });

            // Act & Assert
            match.TestFileMatch("").Should().BeFalse();
        }

        [Fact]
        public void FileMatch_WithMatchingPattern_ShouldReturnTrue()
        {
            // Arrange
            var match = new TestMatch
            {
                Files = new Files()
            };
            match.Files.Name.Add(new Name { Pattern = "*.txt" });

            // Act & Assert
            match.TestFileMatch("test.txt").Should().BeTrue();
        }

        [Fact]
        public void FileMatch_WithNonMatchingPattern_ShouldReturnFalse()
        {
            // Arrange
            var match = new TestMatch
            {
                Files = new Files()
            };
            match.Files.Name.Add(new Name { Pattern = "*.txt" });

            // Act & Assert
            match.TestFileMatch("test.log").Should().BeFalse();
        }

        [Fact]
        public void FileMatch_WithMultiplePatterns_ShouldMatchAny()
        {
            // Arrange
            var match = new TestMatch
            {
                Files = new Files()
            };
            match.Files.Name.Add(new Name { Pattern = "*.txt" });
            match.Files.Name.Add(new Name { Pattern = "*.log" });

            // Act & Assert
            match.TestFileMatch("test.log").Should().BeTrue();
        }

        [Fact]
        public void AttributeMatch_WithNullAttributes_ShouldReturnFalse()
        {
            // Arrange
            var match = new TestMatch();
            var testFile = CreateTestFile("test.txt");

            // Act & Assert
            match.TestAttributeMatch(testFile).Should().BeFalse();
        }

        [Fact]
        public void AttributeMatch_WithEmptyAttributes_ShouldReturnFalse()
        {
            // Arrange
            var match = new TestMatch
            {
                Attributes = new Attributes()
            };
            var testFile = CreateTestFile("test.txt");

            // Act & Assert
            match.TestAttributeMatch(testFile).Should().BeFalse();
        }

        [Fact]
        public void AttributeMatch_WithNullPath_ShouldReturnFalse()
        {
            // Arrange
            var match = new TestMatch
            {
                Attributes = new Attributes()
            };
            match.Attributes.AttributeStrings.Add("Normal");

            // Act & Assert
            match.TestAttributeMatch(null!).Should().BeFalse();
        }

        [Fact]
        public void AttributeMatch_WithNonExistentPath_ShouldReturnFalse()
        {
            // Arrange
            var match = new TestMatch
            {
                Attributes = new Attributes()
            };
            match.Attributes.AttributeStrings.Add("Normal");

            // Act & Assert
            match.TestAttributeMatch("C:\\nonexistent.txt").Should().BeFalse();
        }

        [Fact]
        public void Log_ShouldBeSettable()
        {
            // Arrange
            var match = new TestMatch();

            // Act
            match.Log = false;

            // Assert
            match.Log.Should().BeFalse();
        }

        [Fact]
        public void MatchBase_ShouldSetFilesCorrectly()
        {
            // Arrange
            var match = new TestMatch();
            var files = new Files();

            // Act
            match.Files = files;

            // Assert
            match.Files.Should().BeSameAs(files);
        }

        [Fact]
        public void MatchBase_ShouldSetFoldersCorrectly()
        {
            // Arrange
            var match = new TestMatch();
            var folders = new Folders();

            // Act
            match.Folders = folders;

            // Assert
            match.Folders.Should().BeSameAs(folders);
        }

        [Fact]
        public void MatchBase_ShouldSetPathsCorrectly()
        {
            // Arrange
            var match = new TestMatch();
            var paths = new Paths();

            // Act
            match.Paths = paths;

            // Assert
            match.Paths.Should().BeSameAs(paths);
        }

        [Fact]
        public void MatchBase_ShouldSetAttributesCorrectly()
        {
            // Arrange
            var match = new TestMatch();
            var attributes = new Attributes();

            // Act
            match.Attributes = attributes;

            // Assert
            match.Attributes.Should().BeSameAs(attributes);
        }
    }
}
