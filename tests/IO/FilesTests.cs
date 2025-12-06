using FluentAssertions;
using TE.FileWatcher.IO;
using Xunit;

namespace FileWatcher.Tests.IO
{
    public class FilesTests
    {
        [Fact]
        public void Files_ShouldInitializeWithEmptyNameSet()
        {
            // Act
            var files = new Files();

            // Assert
            files.Name.Should().NotBeNull();
            files.Name.Should().BeEmpty();
        }

        [Fact]
        public void Files_ShouldAllowAddingNames()
        {
            // Arrange
            var files = new Files();
            var name1 = new Name { Pattern = "*.txt" };
            var name2 = new Name { Pattern = "*.log" };

            // Act
            files.Name.Add(name1);
            files.Name.Add(name2);

            // Assert
            files.Name.Should().HaveCount(2);
            files.Name.Should().Contain(name1);
            files.Name.Should().Contain(name2);
        }

        [Fact]
        public void Files_ShouldPreventDuplicateNames()
        {
            // Arrange
            var files = new Files();
            var name1 = new Name { Pattern = "*.txt" };
            var name2 = new Name { Pattern = "*.txt" }; // Duplicate

            // Act
            files.Name.Add(name1);
            files.Name.Add(name2);

            // Assert
            files.Name.Should().HaveCount(1); // HashSet prevents duplicates
        }

        [Fact]
        public void Files_ShouldAllowRemovingNames()
        {
            // Arrange
            var files = new Files();
            var name = new Name { Pattern = "*.txt" };
            files.Name.Add(name);

            // Act
            files.Name.Remove(name);

            // Assert
            files.Name.Should().BeEmpty();
        }

        [Fact]
        public void Files_ShouldClearAllNames()
        {
            // Arrange
            var files = new Files();
            files.Name.Add(new Name { Pattern = "*.txt" });
            files.Name.Add(new Name { Pattern = "*.log" });

            // Act
            files.Name.Clear();

            // Assert
            files.Name.Should().BeEmpty();
        }
    }
}
