using FluentAssertions;
using TE.FileWatcher.IO;
using Xunit;

namespace FileWatcher.Tests.IO
{
    public class PathsTests
    {
        [Fact]
        public void Paths_ShouldInitializeWithEmptyPathSet()
        {
            // Act
            var paths = new Paths();

            // Assert
            paths.Path.Should().NotBeNull();
            paths.Path.Should().BeEmpty();
        }

        [Fact]
        public void Paths_ShouldAllowAddingPaths()
        {
            // Arrange
            var paths = new Paths();

            // Act
            paths.Path.Add("C:\\temp");
            paths.Path.Add("C:\\logs");

            // Assert
            paths.Path.Should().HaveCount(2);
            paths.Path.Should().Contain("C:\\temp");
            paths.Path.Should().Contain("C:\\logs");
        }

        [Fact]
        public void Paths_ShouldPreventDuplicatePaths()
        {
            // Arrange
            var paths = new Paths();

            // Act
            paths.Path.Add("C:\\temp");
            paths.Path.Add("C:\\temp"); // Duplicate

            // Assert
            paths.Path.Should().HaveCount(1); // HashSet prevents duplicates
        }

        [Fact]
        public void Paths_ShouldBeCaseInsensitive()
        {
            // Arrange
            var paths = new Paths();

            // Act
            paths.Path.Add("C:\\TEMP");
            paths.Path.Add("c:\\temp"); // Same path, different case

            // Assert
            paths.Path.Should().HaveCount(1); // Case-insensitive comparison
        }

        [Fact]
        public void Paths_ShouldAllowRemovingPaths()
        {
            // Arrange
            var paths = new Paths();
            paths.Path.Add("C:\\temp");

            // Act
            paths.Path.Remove("C:\\temp");

            // Assert
            paths.Path.Should().BeEmpty();
        }

        [Fact]
        public void Paths_ShouldClearAllPaths()
        {
            // Arrange
            var paths = new Paths();
            paths.Path.Add("C:\\temp");
            paths.Path.Add("C:\\logs");

            // Act
            paths.Path.Clear();

            // Assert
            paths.Path.Should().BeEmpty();
        }

        [Fact]
        public void Paths_WithEmptyString_ShouldAcceptIt()
        {
            // Arrange
            var paths = new Paths();

            // Act
            paths.Path.Add("");

            // Assert
            paths.Path.Should().HaveCount(1);
            paths.Path.Should().Contain("");
        }

        [Fact]
        public void Paths_WithRelativePaths_ShouldAcceptThem()
        {
            // Arrange
            var paths = new Paths();

            // Act
            paths.Path.Add("../parent");
            paths.Path.Add("./current");

            // Assert
            paths.Path.Should().HaveCount(2);
            paths.Path.Should().Contain("../parent");
            paths.Path.Should().Contain("./current");
        }
    }
}
