using FluentAssertions;
using Xunit;
using IODirectory = System.IO.Directory;
using IOFile = System.IO.File;
using TFDirectory = TE.FileWatcher.FileSystem.Directory;

namespace FileWatcher.Tests.FileSystem
{
    public class DirectoryTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly List<string> _testDirectories = new();

        public DirectoryTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), $"DirectoryTests_{Guid.NewGuid()}");
            IODirectory.CreateDirectory(_testDirectory);
            _testDirectories.Add(_testDirectory);
        }

        public void Dispose()
        {
            foreach (var dir in _testDirectories.Where(IODirectory.Exists))
            {
                try
                {
                    IODirectory.Delete(dir, true);
                }
                catch { }
            }
        }

        private string GetTestPath(string relativePath)
        {
            var path = Path.Combine(_testDirectory, relativePath);
            _testDirectories.Add(path);
            return path;
        }

        [Fact]
        public void Create_WithValidPath_ShouldCreateDirectory()
        {
            // Arrange
            var path = GetTestPath("newdir\\file.txt");

            // Act
            TFDirectory.Create(path);

            // Assert
            IODirectory.Exists(Path.GetDirectoryName(path)).Should().BeTrue();
        }

        [Fact]
        public void Create_WithNestedPath_ShouldCreateAllDirectories()
        {
            // Arrange
            var path = Path.Combine(_testDirectory, "level1", "level2", "level3", "file.txt");

            // Act
            TFDirectory.Create(path);

            // Assert
            IODirectory.Exists(Path.GetDirectoryName(path)).Should().BeTrue();
            IODirectory.Exists(Path.Combine(_testDirectory, "level1")).Should().BeTrue();
            IODirectory.Exists(Path.Combine(_testDirectory, "level1", "level2")).Should().BeTrue();
        }

        [Fact]
        public void Create_WithExistingDirectory_ShouldNotThrow()
        {
            // Arrange
            var dirPath = GetTestPath("existing");
            IODirectory.CreateDirectory(dirPath);
            var filePath = Path.Combine(dirPath, "file.txt");

            // Act
            System.Action act = () => TFDirectory.Create(filePath);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Create_WithNullPath_ShouldThrowArgumentNullException()
        {
            // Act
            System.Action act = () => TFDirectory.Create(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("path");
        }

        [Fact]
        public void Create_WithEmptyPath_ShouldThrowArgumentNullException()
        {
            // Act
            System.Action act = () => TFDirectory.Create("");

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("path");
        }

        [Fact]
        public void Create_WithWhitespacePath_ShouldThrowArgumentNullException()
        {
            // Act
            System.Action act = () => TFDirectory.Create("   ");

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("path");
        }

        [Fact]
        public void IsValid_WithExistingDirectory_ShouldReturnTrue()
        {
            // Act
            var result = TFDirectory.IsValid(_testDirectory);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValid_WithNonExistentDirectory_ShouldReturnFalse()
        {
            // Arrange
            var path = GetTestPath("nonexistent");

            // Act
            var result = TFDirectory.IsValid(path);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithNullPath_ShouldReturnFalse()
        {
            // Act
            var result = TFDirectory.IsValid(null!);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithEmptyPath_ShouldReturnFalse()
        {
            // Act
            var result = TFDirectory.IsValid("");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithWhitespacePath_ShouldReturnFalse()
        {
            // Act
            var result = TFDirectory.IsValid("   ");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithFilePath_ShouldReturnFalse()
        {
            // Arrange
            var filePath = Path.Combine(_testDirectory, "file.txt");
            IOFile.WriteAllText(filePath, "content");

            // Act
            var result = TFDirectory.IsValid(filePath);

            // Assert
            result.Should().BeFalse();
        }
    }
}
