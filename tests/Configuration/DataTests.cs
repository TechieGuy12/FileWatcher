using FluentAssertions;
using TE.FileWatcher.Configuration;
using TE.FileWatcher.Net;
using Xunit;

namespace FileWatcher.Tests.Configuration
{
    public class DataTests
    {
        [Fact]
        public void Data_ShouldInitializeWithDefaultMimeType()
        {
            // Act
            var data = new Data();

            // Assert
            data.MimeTypeString.Should().Be(Request.JSON_NAME);
        }

        [Fact]
        public void Data_ShouldHaveNullPropertiesByDefault()
        {
            // Act
            var data = new Data();

            // Assert
            data.Headers.Should().BeNull();
            data.Body.Should().BeNull();
        }

        [Theory]
        [InlineData("JSON")]
        [InlineData("XML")]
        public void MimeTypeString_WithValidType_ShouldSetCorrectly(string mimeType)
        {
            // Arrange
            var data = new Data();

            // Act
            data.MimeTypeString = mimeType;

            // Assert
            data.MimeTypeString.Should().Be(mimeType);
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData("text")]
        [InlineData("")]
        public void MimeTypeString_WithInvalidType_ShouldDefaultToJson(string mimeType)
        {
            // Arrange
            var data = new Data();

            // Act
            data.MimeTypeString = mimeType;

            // Assert
            data.MimeTypeString.Should().Be(Request.JSON_NAME);
        }

        [Fact]
        public void Body_ShouldBeSettable()
        {
            // Arrange
            var data = new Data
            {
                Body = "test body content"
            };

            // Assert
            data.Body.Should().Be("test body content");
        }

        [Fact]
        public void Headers_ShouldBeSettable()
        {
            // Arrange
            var headers = new Headers();
            var data = new Data
            {
                Headers = headers
            };

            // Assert
            data.Headers.Should().BeSameAs(headers);
        }

        [Fact]
        public void MimeType_WhenSetToJson_ShouldReturnJsonEnum()
        {
            // Arrange
            var data = new Data
            {
                MimeTypeString = Request.JSON_NAME
            };

            // Act
            var mimeType = data.MimeType;

            // Assert
            mimeType.Should().Be(Request.MimeType.Json);
        }

        [Fact]
        public void MimeType_WhenSetToXml_ShouldReturnXmlEnum()
        {
            // Arrange
            var data = new Data
            {
                MimeTypeString = Request.XML_NAME
            };

            // Act
            var mimeType = data.MimeType;

            // Assert
            mimeType.Should().Be(Request.MimeType.Xml);
        }

        [Fact]
        public void MimeType_WithDefaultValue_ShouldReturnJsonEnum()
        {
            // Arrange
            var data = new Data();

            // Act
            var mimeType = data.MimeType;

            // Assert
            mimeType.Should().Be(Request.MimeType.Json);
        }
    }
}
