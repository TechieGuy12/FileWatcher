using FluentAssertions;
using TE.FileWatcher.Configuration;
using Xunit;

namespace FileWatcher.Tests.Configuration
{
    public class VariableTests
    {
        [Fact]
        public void Variable_ShouldInitializeWithNullProperties()
        {
            // Act
            var variable = new Variable();

            // Assert
            variable.Name.Should().BeNull();
            variable.Value.Should().BeNull();
        }

        [Fact]
        public void Variable_ShouldSetNameCorrectly()
        {
            // Arrange
            var variable = new Variable
            {
                Name = "environment"
            };

            // Assert
            variable.Name.Should().Be("environment");
        }

        [Fact]
        public void Variable_ShouldSetValueCorrectly()
        {
            // Arrange
            var variable = new Variable
            {
                Value = "production"
            };

            // Assert
            variable.Value.Should().Be("production");
        }

        [Fact]
        public void Variable_ShouldSetBothProperties()
        {
            // Arrange
            var variable = new Variable
            {
                Name = "apiKey",
                Value = "secret123"
            };

            // Assert
            variable.Name.Should().Be("apiKey");
            variable.Value.Should().Be("secret123");
        }

        [Fact]
        public void Variable_WithEmptyStrings_ShouldNotBeNull()
        {
            // Arrange
            var variable = new Variable
            {
                Name = "",
                Value = ""
            };

            // Assert
            variable.Name.Should().NotBeNull();
            variable.Value.Should().NotBeNull();
            variable.Name.Should().BeEmpty();
            variable.Value.Should().BeEmpty();
        }

        [Fact]
        public void Variable_WithWhitespace_ShouldPreserveWhitespace()
        {
            // Arrange
            var variable = new Variable
            {
                Name = "  spaced  ",
                Value = "  value  "
            };

            // Assert
            variable.Name.Should().Be("  spaced  ");
            variable.Value.Should().Be("  value  ");
        }
    }
}
