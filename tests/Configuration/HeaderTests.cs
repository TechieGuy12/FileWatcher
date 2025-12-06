using FluentAssertions;
using TE.FileWatcher.Configuration;
using Xunit;

namespace FileWatcher.Tests.Configuration
{
    public class HeaderTests
    {
        [Fact]
        public void Header_ShouldInitializeWithNullProperties()
        {
            // Act
            var header = new Header();

            // Assert
            header.Name.Should().BeNull();
            header.Value.Should().BeNull();
        }

        [Fact]
        public void Header_ShouldSetNameCorrectly()
        {
            // Arrange
            var header = new Header
            {
                Name = "Authorization"
            };

            // Assert
            header.Name.Should().Be("Authorization");
        }

        [Fact]
        public void Header_ShouldSetValueCorrectly()
        {
            // Arrange
            var header = new Header
            {
                Value = "Bearer token123"
            };

            // Assert
            header.Value.Should().Be("Bearer token123");
        }

        [Fact]
        public void Header_ShouldSetBothProperties()
        {
            // Arrange
            var header = new Header
            {
                Name = "Content-Type",
                Value = "application/json"
            };

            // Assert
            header.Name.Should().Be("Content-Type");
            header.Value.Should().Be("application/json");
        }

        [Fact]
        public void Header_WithEmptyStrings_ShouldNotBeNull()
        {
            // Arrange
            var header = new Header
            {
                Name = "",
                Value = ""
            };

            // Assert
            header.Name.Should().NotBeNull();
            header.Value.Should().NotBeNull();
            header.Name.Should().BeEmpty();
            header.Value.Should().BeEmpty();
        }
    }
}
