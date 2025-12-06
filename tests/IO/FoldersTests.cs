using FluentAssertions;
using TE.FileWatcher.IO;
using Xunit;

namespace FileWatcher.Tests.IO
{
    public class FoldersTests
    {
        [Fact]
        public void Folders_ShouldInitializeWithEmptyNameSet()
        {
            // Act
            var folders = new Folders();

            // Assert
            folders.Name.Should().NotBeNull();
            folders.Name.Should().BeEmpty();
        }

        [Fact]
        public void Folders_ShouldAllowAddingNames()
        {
            // Arrange
            var folders = new Folders();
            var name1 = new Name { Pattern = "temp" };
            var name2 = new Name { Pattern = ".git" };

            // Act
            folders.Name.Add(name1);
            folders.Name.Add(name2);

            // Assert
            folders.Name.Should().HaveCount(2);
            folders.Name.Should().Contain(name1);
            folders.Name.Should().Contain(name2);
        }

        [Fact]
        public void Folders_ShouldPreventDuplicateNames()
        {
            // Arrange
            var folders = new Folders();
            var name1 = new Name { Pattern = "temp" };
            var name2 = new Name { Pattern = "temp" }; // Duplicate

            // Act
            folders.Name.Add(name1);
            folders.Name.Add(name2);

            // Assert
            folders.Name.Should().HaveCount(1); // HashSet prevents duplicates
        }

        [Fact]
        public void Folders_ShouldAllowRemovingNames()
        {
            // Arrange
            var folders = new Folders();
            var name = new Name { Pattern = "temp" };
            folders.Name.Add(name);

            // Act
            folders.Name.Remove(name);

            // Assert
            folders.Name.Should().BeEmpty();
        }

        [Fact]
        public void Folders_ShouldClearAllNames()
        {
            // Arrange
            var folders = new Folders();
            folders.Name.Add(new Name { Pattern = "temp" });
            folders.Name.Add(new Name { Pattern = ".git" });

            // Act
            folders.Name.Clear();

            // Assert
            folders.Name.Should().BeEmpty();
        }
    }
}
