using FluentAssertions;
using TE.FileWatcher.IO;
using Xunit;

namespace FileWatcher.Tests.IO
{
    public class NameTests
    {
        [Fact]
        public void Name_ShouldInitializeWithNullPattern()
        {
            // Act
            var name = new Name();

            // Assert
            name.Pattern.Should().BeNull();
        }

        [Fact]
        public void Pattern_ShouldBeSettable()
        {
            // Arrange
            var name = new Name
            {
                Pattern = "*.txt"
            };

            // Assert
            name.Pattern.Should().Be("*.txt");
        }

        [Fact]
        public void IsMatch_WithNullValue_ShouldThrowArgumentNullException()
        {
            // Arrange
            var name = new Name { Pattern = "*.txt" };

            // Act
            System.Action act = () => name.IsMatch(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void IsMatch_WithEmptyValue_ShouldThrowArgumentNullException()
        {
            // Arrange
            var name = new Name { Pattern = "*.txt" };

            // Act
            System.Action act = () => name.IsMatch("");

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void IsMatch_WithWhitespaceValue_ShouldThrowArgumentNullException()
        {
            // Arrange
            var name = new Name { Pattern = "*.txt" };

            // Act
            System.Action act = () => name.IsMatch("   ");

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void IsMatch_WithNullPattern_ShouldReturnFalse()
        {
            // Arrange
            var name = new Name { Pattern = null };

            // Act
            var result = name.IsMatch("test.txt");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsMatch_WithEmptyPattern_ShouldReturnFalse()
        {
            // Arrange
            var name = new Name { Pattern = "" };

            // Act
            var result = name.IsMatch("test.txt");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_WithSamePattern_ShouldBeEqual()
        {
            // Arrange
            var name1 = new Name { Pattern = "*.txt" };
            var name2 = new Name { Pattern = "*.txt" };

            // Act & Assert
            name1.Equals(name2).Should().BeTrue();
            name1.GetHashCode().Should().Be(name2.GetHashCode());
        }

        [Fact]
        public void Equals_WithDifferentPattern_ShouldNotBeEqual()
        {
            // Arrange
            var name1 = new Name { Pattern = "*.txt" };
            var name2 = new Name { Pattern = "*.log" };

            // Act & Assert
            name1.Equals(name2).Should().BeFalse();
        }

        [Fact]
        public void HashSet_ShouldHandleNameObjects()
        {
            // Arrange
            var set = new HashSet<Name>
            {
                new Name { Pattern = "*.txt" },
                new Name { Pattern = "*.log" },
                new Name { Pattern = "*.txt" } // Duplicate
            };

            // Assert
            set.Should().HaveCount(2); // Should only have 2 unique patterns
        }
    }
}
