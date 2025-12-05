using FluentAssertions;
using TE.FileWatcher.Configuration;
using TE.FileWatcher.IO;
using Xunit;

namespace FileWatcher.Tests.Configuration
{
    public class ExclusionsTests
    {
        [Fact]
        public void Exclusions_ShouldInitializeWithNullProperties()
        {
            // Act
            var exclusions = new Exclusions();

            // Assert
            exclusions.Files.Should().BeNull();
            exclusions.Folders.Should().BeNull();
            exclusions.Paths.Should().BeNull();
            exclusions.Attributes.Should().BeNull();
        }

        [Fact]
        public void IsSpecified_WithNoExclusions_ShouldReturnFalse()
        {
            // Arrange
            var exclusions = new Exclusions();

            // Act
            var result = exclusions.IsSpecified();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsSpecified_WithFileExclusion_ShouldReturnTrue()
        {
            // Arrange
            var exclusions = new Exclusions
            {
                Files = new Files
                {
                    Name = new HashSet<Name>
                    {
                        new Name { Pattern = "*.tmp" }
                    }
                }
            };

            // Act
            var result = exclusions.IsSpecified();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsSpecified_WithFolderExclusion_ShouldReturnTrue()
        {
            // Arrange
            var exclusions = new Exclusions
            {
                Folders = new Folders
                {
                    Name = new HashSet<Name>
                    {
                        new Name { Pattern = ".git" }
                    }
                }
            };

            // Act
            var result = exclusions.IsSpecified();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Exclude_WithNullChange_ShouldThrowArgumentNullException()
        {
            // Arrange
            var exclusions = new Exclusions();

            // Act
            System.Action act = () => exclusions.Exclude(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Log_ShouldDefaultToTrue()
        {
            // Act
            var exclusions = new Exclusions();

            // Assert
            exclusions.Log.Should().BeTrue();
        }

        [Fact]
        public void IsSpecified_WithAttributeExclusion_ShouldReturnTrue()
        {
            // Arrange
            var attributes = new TE.FileWatcher.IO.Attributes();
            attributes.AttributeStrings.Add("Hidden");
            
            var exclusions = new Exclusions
            {
                Attributes = attributes
            };

            // Act
            var result = exclusions.IsSpecified();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsSpecified_WithMultipleExclusions_ShouldReturnTrue()
        {
            // Arrange
            var exclusions = new Exclusions
            {
                Files = new Files
                {
                    Name = new HashSet<Name>
                    {
                        new Name { Pattern = "*.tmp" }
                    }
                },
                Folders = new Folders
                {
                    Name = new HashSet<Name>
                    {
                        new Name { Pattern = ".git" }
                    }
                }
            };

            // Act
            var result = exclusions.IsSpecified();

            // Assert
            result.Should().BeTrue();
        }
    }
}
