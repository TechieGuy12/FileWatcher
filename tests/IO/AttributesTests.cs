using FluentAssertions;
using TE.FileWatcher.IO;
using Xunit;

namespace FileWatcher.Tests.IO
{
    public class AttributesTests
    {
        [Fact]
        public void Attributes_ShouldInitializeWithEmptyHashSet()
        {
            // Act
            var attributes = new Attributes();

            // Assert
            attributes.AttributeStrings.Should().NotBeNull();
            attributes.AttributeStrings.Should().BeEmpty();
        }

        [Fact]
        public void Attributes_Attribute_WithNoStrings_ShouldReturnEmpty()
        {
            // Arrange
            var attributes = new Attributes();

            // Act
            var result = attributes.Attribute;

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public void Attributes_Attribute_WithValidString_ShouldParse()
        {
            // Arrange
            var attributes = new Attributes();
            attributes.AttributeStrings.Add("Hidden");

            // Act
            var result = attributes.Attribute;

            // Assert
            result.Should().Contain(FileAttributes.Hidden);
        }

        [Fact]
        public void Attributes_Attribute_WithMultipleValidStrings_ShouldParseAll()
        {
            // Arrange
            var attributes = new Attributes();
            attributes.AttributeStrings.Add("Hidden");
            attributes.AttributeStrings.Add("ReadOnly");
            attributes.AttributeStrings.Add("System");

            // Act
            var result = attributes.Attribute;

            // Assert
            result.Should().HaveCount(3);
            result.Should().Contain(FileAttributes.Hidden);
            result.Should().Contain(FileAttributes.ReadOnly);
            result.Should().Contain(FileAttributes.System);
        }

        [Fact]
        public void Attributes_Attribute_WithInvalidString_ShouldSkip()
        {
            // Arrange
            var attributes = new Attributes();
            attributes.AttributeStrings.Add("Invalid");
            attributes.AttributeStrings.Add("Hidden");

            // Act
            var result = attributes.Attribute;

            // Assert
            result.Should().HaveCount(1);
            result.Should().Contain(FileAttributes.Hidden);
        }

        [Fact]
        public void Attributes_Attribute_WithEmptyString_ShouldSkip()
        {
            // Arrange
            var attributes = new Attributes();
            attributes.AttributeStrings.Add("");
            attributes.AttributeStrings.Add("Hidden");

            // Act
            var result = attributes.Attribute;

            // Assert
            result.Should().HaveCount(1);
        }

        [Fact]
        public void Attributes_AttributeStrings_ShouldBeModifiable()
        {
            // Arrange
            var attributes = new Attributes();

            // Act
            attributes.AttributeStrings.Add("Hidden");
            attributes.AttributeStrings.Add("ReadOnly");

            // Assert
            attributes.AttributeStrings.Should().HaveCount(2);
        }

        [Fact]
        public void Attributes_Attribute_CalledMultipleTimes_ShouldReturnSameValues()
        {
            // Arrange
            var attributes = new Attributes();
            attributes.AttributeStrings.Add("Hidden");

            // Act
            var result1 = attributes.Attribute;
            var result2 = attributes.Attribute;

            // Assert
            result1.Should().BeEquivalentTo(result2);
        }

        [Fact]
        public void Attributes_Attribute_WithDuplicates_ShouldHandleHashSet()
        {
            // Arrange
            var attributes = new Attributes();
            attributes.AttributeStrings.Add("Hidden");
            attributes.AttributeStrings.Add("Hidden");

            // Act
            var result = attributes.Attribute;

            // Assert
            result.Should().HaveCount(1);
        }

        [Fact]
        public void Attributes_Attribute_WithNormal_ShouldParse()
        {
            // Arrange
            var attributes = new Attributes();
            attributes.AttributeStrings.Add("Normal");

            // Act
            var result = attributes.Attribute;

            // Assert
            result.Should().Contain(FileAttributes.Normal);
        }

        [Fact]
        public void Attributes_Attribute_WithArchive_ShouldParse()
        {
            // Arrange
            var attributes = new Attributes();
            attributes.AttributeStrings.Add("Archive");

            // Act
            var result = attributes.Attribute;

            // Assert
            result.Should().Contain(FileAttributes.Archive);
        }

        [Fact]
        public void Attributes_Attribute_WithDirectory_ShouldParse()
        {
            // Arrange
            var attributes = new Attributes();
            attributes.AttributeStrings.Add("Directory");

            // Act
            var result = attributes.Attribute;

            // Assert
            result.Should().Contain(FileAttributes.Directory);
        }

        [Fact]
        public void Attributes_Attribute_WithTemporary_ShouldParse()
        {
            // Arrange
            var attributes = new Attributes();
            attributes.AttributeStrings.Add("Temporary");

            // Act
            var result = attributes.Attribute;

            // Assert
            result.Should().Contain(FileAttributes.Temporary);
        }

        [Fact]
        public void Attributes_Attribute_WithCompressed_ShouldParse()
        {
            // Arrange
            var attributes = new Attributes();
            attributes.AttributeStrings.Add("Compressed");

            // Act
            var result = attributes.Attribute;

            // Assert
            result.Should().Contain(FileAttributes.Compressed);
        }
    }
}
