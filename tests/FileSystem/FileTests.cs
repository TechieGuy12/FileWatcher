using FluentAssertions;
using TE.FileWatcher.FileSystem;
using Xunit;
using TFFile = TE.FileWatcher.FileSystem.File;
using IOFile = System.IO.File;
using IODirectory = System.IO.Directory;

namespace FileWatcher.Tests.FileSystem
{
    public class FileTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly List<string> _testFiles = new();

        public FileTests()
        {
            // Create a temporary test directory
            _testDirectory = Path.Combine(Path.GetTempPath(), $"FileWatcherTests_{Guid.NewGuid()}");
            IODirectory.CreateDirectory(_testDirectory);
        }

        public void Dispose()
        {
            // Cleanup test files and directory
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
        public void GetName_WithExtension_ShouldReturnFullFilename()
        {
            // Arrange
            var testFile = CreateTestFile("testfile.txt");

            // Act
            var result = TFFile.GetName(testFile, includeExtension: true);

            // Assert
            result.Should().Be("testfile.txt");
        }

        [Fact]
        public void GetName_WithoutExtension_ShouldReturnFilenameOnly()
        {
            // Arrange
            var testFile = CreateTestFile("testfile.txt");

            // Act
            var result = TFFile.GetName(testFile, includeExtension: false);

            // Assert
            result.Should().Be("testfile");
        }

        [Fact]
        public void GetName_MultipleExtensions_WithExtension_ShouldReturnFullFilename()
        {
            // Arrange
            var testFile = CreateTestFile("archive.tar.gz");

            // Act
            var result = TFFile.GetName(testFile, includeExtension: true);

            // Assert
            result.Should().Be("archive.tar.gz");
        }

        [Fact]
        public void GetName_MultipleExtensions_WithoutExtension_ShouldReturnFilenameWithoutLastExtension()
        {
            // Arrange
            var testFile = CreateTestFile("archive.tar.gz");

            // Act
            var result = TFFile.GetName(testFile, includeExtension: false);

            // Assert
            result.Should().Be("archive.tar");
        }

        [Fact]
        public void GetName_NonExistentFile_ShouldReturnNull()
        {
            // Arrange
            var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.txt");

            // Act
            var result = TFFile.GetName(nonExistentPath, includeExtension: true);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetName_NullPath_ShouldReturnNull()
        {
            // Act
            var result = TFFile.GetName(null!, includeExtension: true);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetName_EmptyPath_ShouldReturnNull()
        {
            // Act
            var result = TFFile.GetName(string.Empty, includeExtension: true);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetExtension_ValidFile_ShouldReturnExtension()
        {
            // Arrange
            var testFile = CreateTestFile("document.pdf");

            // Act
            var result = TFFile.GetExtension(testFile);

            // Assert
            result.Should().Be(".pdf");
        }

        [Fact]
        public void GetExtension_NoExtension_ShouldReturnEmptyString()
        {
            // Arrange
            var testFile = CreateTestFile("README");

            // Act
            var result = TFFile.GetExtension(testFile);

            // Assert
            result.Should().Be("");
        }

        [Fact]
        public void IsValid_ExistingFile_ShouldReturnTrue()
        {
            // Arrange
            var testFile = CreateTestFile("valid.txt");

            // Act
            var result = TFFile.IsValid(testFile);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValid_NonExistentFile_ShouldReturnFalse()
        {
            // Arrange
            var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.txt");

            // Act
            var result = TFFile.IsValid(nonExistentPath);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_NullPath_ShouldReturnFalse()
        {
            // Act
            var result = TFFile.IsValid(null!);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Copy_ValidSourceAndDestination_ShouldCopyFile()
        {
            // Arrange
            var sourceFile = CreateTestFile("source.txt");
            var destinationFile = Path.Combine(_testDirectory, "destination.txt");
            _testFiles.Add(destinationFile);

            // Act
            TFFile.Copy(sourceFile, destinationFile, verify: false, keepTimestamp: false);

            // Assert
            IOFile.Exists(destinationFile).Should().BeTrue();
            IOFile.ReadAllText(destinationFile).Should().Be("test content");
        }

        [Fact]
        public void Delete_ExistingFile_ShouldDeleteFile()
        {
            // Arrange
            var testFile = CreateTestFile("todelete.txt");

            // Act
            TFFile.Delete(testFile);

            // Assert
            IOFile.Exists(testFile).Should().BeFalse();
        }

        [Fact]
        public void Delete_NonExistentFile_ShouldNotThrow()
        {
            // Arrange
            var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.txt");

            // Act
            System.Action act = () => TFFile.Delete(nonExistentPath);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Move_ValidSourceAndDestination_ShouldMoveFile()
        {
            // Arrange
            var sourceFile = CreateTestFile("tomove.txt");
            var destinationFile = Path.Combine(_testDirectory, "moved.txt");
            _testFiles.Add(destinationFile);

            // Act
            TFFile.Move(sourceFile, destinationFile, verify: false, keepTimestamp: false);

            // Assert
            IOFile.Exists(sourceFile).Should().BeFalse();
            IOFile.Exists(destinationFile).Should().BeTrue();
        }
    }
}
