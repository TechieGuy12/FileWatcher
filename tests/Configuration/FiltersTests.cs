using FluentAssertions;
using TE.FileWatcher.Configuration;
using TE.FileWatcher.IO;
using Xunit;
using IOFile = System.IO.File;
using IODirectory = System.IO.Directory;

namespace FileWatcher.Tests.Configuration
{
    public class FiltersTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly List<string> _testFiles = new();

        public FiltersTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), $"FiltersTests_{Guid.NewGuid()}");
            IODirectory.CreateDirectory(_testDirectory);
        }

        public void Dispose()
        {
            foreach (var file in _testFiles.Where(IOFile.Exists))
            {
                try
                {
                    IOFile.Delete(file);
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
            IOFile.WriteAllText(path, "test content");
            _testFiles.Add(path);
            return path;
        }

        [Fact]
        public void Filters_ShouldInitializeWithNullProperties()
        {
            // Act
            var filters = new Filters();

            // Assert
            filters.Files.Should().BeNull();
            filters.Folders.Should().BeNull();
            filters.Paths.Should().BeNull();
            filters.Attributes.Should().BeNull();
        }

        [Fact]
        public void IsSpecified_WithNoFilters_ShouldReturnFalse()
        {
            // Arrange
            var filters = new Filters();

            // Act
            var result = filters.IsSpecified();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsSpecified_WithFileFilter_ShouldReturnTrue()
        {
            // Arrange
            var filters = new Filters
            {
                Files = new Files
                {
                    Name = new HashSet<Name>
                    {
                        new Name { Pattern = "*.txt" }
                    }
                }
            };

            // Act
            var result = filters.IsSpecified();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsSpecified_WithFolderFilter_ShouldReturnTrue()
        {
            // Arrange
            var filters = new Filters
            {
                Folders = new Folders
                {
                    Name = new HashSet<Name>
                    {
                        new Name { Pattern = "temp" }
                    }
                }
            };

            // Act
            var result = filters.IsSpecified();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsSpecified_WithPathFilter_ShouldReturnTrue()
        {
            // Arrange
            var filters = new Filters
            {
                Paths = new Paths
                {
                    Path = new HashSet<string>
                    {
                        "C:\\temp"
                    }
                }
            };

            // Act
            var result = filters.IsSpecified();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsMatch_WithNullChange_ShouldThrowArgumentNullException()
        {
            // Arrange
            var filters = new Filters();

            // Act
            System.Action act = () => filters.IsMatch(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Log_ShouldDefaultToTrue()
        {
            // Act
            var filters = new Filters();

            // Assert
            filters.Log.Should().BeTrue();
        }

        [Fact]
        public void Log_ShouldBeSettable()
        {
            // Arrange
            var filters = new Filters
            {
                Log = false
            };

            // Assert
            filters.Log.Should().BeFalse();
        }
    }
}
