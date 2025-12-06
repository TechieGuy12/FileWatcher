using FluentAssertions;
using TE.FileWatcher.Configuration;
using Xunit;

namespace FileWatcher.Tests.Configuration
{
    public class NotificationTests
    {
        [Fact]
        public void Notification_ShouldInitializeWithNullProperties()
        {
            // Act
            var notification = new Notification();

            // Assert
            notification.Url.Should().BeNull();
            notification.MethodString.Should().BeNull();
            notification.Data.Should().BeNull();
        }

        [Fact]
        public void Notification_ShouldInitializeWithNoMessage()
        {
            // Act
            var notification = new Notification();

            // Assert
            notification.HasMessage.Should().BeFalse();
        }

        [Fact]
        public void Notification_ShouldSetUrlCorrectly()
        {
            // Arrange & Act
            var notification = new Notification
            {
                Url = "https://api.example.com/webhook"
            };

            // Assert
            notification.Url.Should().Be("https://api.example.com/webhook");
        }

        [Fact]
        public void Notification_ShouldSetMethodStringCorrectly()
        {
            // Arrange & Act
            var notification = new Notification
            {
                MethodString = "POST"
            };

            // Assert
            notification.MethodString.Should().Be("POST");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("invalid")]
        [InlineData("patch")]
        public void Method_WithNullOrInvalidString_ShouldDefaultToPost(string? methodString)
        {
            // Arrange
            var notification = new Notification
            {
                MethodString = methodString
            };

            // Act
            var method = notification.Method;

            // Assert
            method.Should().Be(HttpMethod.Post);
        }

        [Theory]
        [InlineData("get", "GET")]
        [InlineData("GET", "GET")]
        [InlineData("Get", "GET")]
        [InlineData("GEt", "GET")]
        public void Method_WithGetVariations_ShouldReturnGet(string methodString, string expectedMethod)
        {
            // Arrange
            var notification = new Notification
            {
                MethodString = methodString
            };

            // Act
            var method = notification.Method;

            // Assert
            method.Method.Should().Be(expectedMethod);
        }

        [Theory]
        [InlineData("post", "POST")]
        [InlineData("POST", "POST")]
        [InlineData("Post", "POST")]
        public void Method_WithPostVariations_ShouldReturnPost(string methodString, string expectedMethod)
        {
            // Arrange
            var notification = new Notification
            {
                MethodString = methodString
            };

            // Act
            var method = notification.Method;

            // Assert
            method.Method.Should().Be(expectedMethod);
        }

        [Theory]
        [InlineData("delete", "DELETE")]
        [InlineData("DELETE", "DELETE")]
        [InlineData("Delete", "DELETE")]
        public void Method_WithDeleteVariations_ShouldReturnDelete(string methodString, string expectedMethod)
        {
            // Arrange
            var notification = new Notification
            {
                MethodString = methodString
            };

            // Act
            var method = notification.Method;

            // Assert
            method.Method.Should().Be(expectedMethod);
        }

        [Theory]
        [InlineData("put", "PUT")]
        [InlineData("PUT", "PUT")]
        [InlineData("Put", "PUT")]
        public void Method_WithPutVariations_ShouldReturnPut(string methodString, string expectedMethod)
        {
            // Arrange
            var notification = new Notification
            {
                MethodString = methodString
            };

            // Act
            var method = notification.Method;

            // Assert
            method.Method.Should().Be(expectedMethod);
        }

        [Fact]
        public void Notification_ShouldSetDataCorrectly()
        {
            // Arrange
            var data = new Data
            {
                Body = "test body"
            };

            // Act
            var notification = new Notification
            {
                Data = data
            };

            // Assert
            notification.Data.Should().NotBeNull();
            notification.Data.Should().BeSameAs(data);
        }

        [Fact]
        public void Notification_WithAllPropertiesSet_ShouldRetainValues()
        {
            // Arrange
            var data = new Data { Body = "body" };

            // Act
            var notification = new Notification
            {
                Url = "https://example.com",
                MethodString = "POST",
                Data = data
            };

            // Assert
            notification.Url.Should().Be("https://example.com");
            notification.MethodString.Should().Be("POST");
            notification.Data.Should().BeSameAs(data);
        }
    }
}
