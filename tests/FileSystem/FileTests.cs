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
        public void Copy_ValidSourceAndDestination_ShouldCopyFile()
        {
            // Arrange
            var source = CreateTestFile("source.txt");
            var dest = Path.Combine(_testDirectory, "dest.txt");
            _testFiles.Add(dest);

            // Act
            TFFile.Copy(source, dest, verify: false, keepTimestamp: false);

            // Assert
            IOFile.Exists(dest).Should().BeTrue();
        }

        [Fact]
        public void Copy_WithVerify_ShouldCopyAndVerify()
        {
            // Arrange
            var source = CreateTestFile("source.txt");
            var dest = Path.Combine(_testDirectory, "dest.txt");
            _testFiles.Add(dest);

            // Act
            TFFile.Copy(source, dest, verify: true, keepTimestamp: false);

            // Assert
            IOFile.Exists(dest).Should().BeTrue();
            IOFile.ReadAllText(dest).Should().Be("test content");
        }

        [Fact]
        public void Copy_WithKeepTimestamp_ShouldPreserveTimestamps()
        {
            // Arrange
            var source = CreateTestFile("source.txt");
            var sourceCreated = IOFile.GetCreationTime(source);
            var sourceModified = IOFile.GetLastWriteTime(source);
            var dest = Path.Combine(_testDirectory, "dest.txt");
            _testFiles.Add(dest);

            // Act
            TFFile.Copy(source, dest, verify: false, keepTimestamp: true);

            // Assert
            var destCreated = IOFile.GetCreationTime(dest);
            var destModified = IOFile.GetLastWriteTime(dest);
            destCreated.Should().BeCloseTo(sourceCreated, TimeSpan.FromSeconds(1));
            destModified.Should().BeCloseTo(sourceModified, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Copy_WithNullSource_ShouldThrowArgumentNullException()
        {
            // Act
            System.Action act = () => TFFile.Copy(null!, "dest.txt", false, false);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("source");
        }

        [Fact]
        public void Copy_WithNullDestination_ShouldThrowArgumentNullException()
        {
            // Arrange
            var source = CreateTestFile("source.txt");

            // Act
            System.Action act = () => TFFile.Copy(source, null!, false, false);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("destination");
        }

        [Fact]
        public void Copy_WithNonExistentSource_ShouldNotThrow()
        {
            // Arrange
            var source = Path.Combine(_testDirectory, "nonexistent.txt");
            var dest = Path.Combine(_testDirectory, "dest.txt");

            // Act
            System.Action act = () => TFFile.Copy(source, dest, false, false);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Copy_ToNestedDirectory_ShouldCreateDirectoryStructure()
        {
            // Arrange
            var source = CreateTestFile("source.txt");
            var dest = Path.Combine(_testDirectory, "nested", "folder", "dest.txt");
            _testFiles.Add(dest);

            // Act
            TFFile.Copy(source, dest, verify: false, keepTimestamp: false);

            // Assert
            IOFile.Exists(dest).Should().BeTrue();
        }

        [Fact]
        public void Move_ValidSourceAndDestination_ShouldMoveFile()
        {
            // Arrange
            var source = CreateTestFile("source.txt");
            var dest = Path.Combine(_testDirectory, "dest.txt");
            _testFiles.Add(dest);

            // Act
            TFFile.Move(source, dest, verify: false, keepTimestamp: false);

            // Assert
            IOFile.Exists(dest).Should().BeTrue();
            IOFile.Exists(source).Should().BeFalse();
        }

        [Fact]
        public void Move_WithVerify_ShouldMoveAndVerify()
        {
            // Arrange
            var source = CreateTestFile("source.txt");
            var dest = Path.Combine(_testDirectory, "dest.txt");
            _testFiles.Add(dest);

            // Act
            TFFile.Move(source, dest, verify: true, keepTimestamp: false);

            // Assert
            IOFile.Exists(dest).Should().BeTrue();
            IOFile.ReadAllText(dest).Should().Be("test content");
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
        public void Delete_WithNullPath_ShouldThrowArgumentNullException()
        {
            // Act
            System.Action act = () => TFFile.Delete(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GetCreatedDate_ValidFile_ShouldReturnCreationDate()
        {
            // Arrange
            var testFile = CreateTestFile("test.txt");

            // Act
            var result = TFFile.GetCreatedDate(testFile);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void GetCreatedDate_NonExistentFile_ShouldReturnNull()
        {
            // Arrange
            var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.txt");

            // Act
            var result = TFFile.GetCreatedDate(nonExistentPath);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetCreatedDate_NullPath_ShouldReturnNull()
        {
            // Act
            var result = TFFile.GetCreatedDate(null!);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetModifiedDate_ValidFile_ShouldReturnModifiedDate()
        {
            // Arrange
            var testFile = CreateTestFile("test.txt");

            // Act
            var result = TFFile.GetModifiedDate(testFile);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void GetModifiedDate_NonExistentFile_ShouldReturnNull()
        {
            // Arrange
            var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.txt");

            // Act
            var result = TFFile.GetModifiedDate(nonExistentPath);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetModifiedDate_NullPath_ShouldReturnNull()
        {
            // Act
            var result = TFFile.GetModifiedDate(null!);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void IsValid_ExistingFile_ShouldReturnTrue()
        {
            // Arrange
            var testFile = CreateTestFile("test.txt");

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
        public void IsValid_EmptyPath_ShouldReturnFalse()
        {
            // Act
            var result = TFFile.IsValid(string.Empty);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_DirectoryPath_ShouldReturnFalse()
        {
            // Act
            var result = TFFile.IsValid(_testDirectory);

            // Assert
            result.Should().BeFalse();
        }
    }
}
