using FluentAssertions;
using System.Collections.ObjectModel;
using TE.FileWatcher.Configuration;
using Xunit;

namespace FileWatcher.Tests.Configuration
{
    public class HeadersTests
    {
        [Fact]
        public void Headers_ShouldInitializeWithNullHeaderList()
        {
            // Act
            var headers = new Headers();

            // Assert
            headers.HeaderList.Should().BeNull();
        }

        [Fact]
        public void Headers_ShouldSetHeaderList()
        {
            // Arrange
            var headerList = new Collection<Header>
            {
                new Header { Name = "Content-Type", Value = "application/json" },
                new Header { Name = "Authorization", Value = "Bearer token" }
            };

            // Act
            var headers = new Headers
            {
                HeaderList = headerList
            };

            // Assert
            headers.HeaderList.Should().NotBeNull();
            headers.HeaderList.Should().HaveCount(2);
            headers.HeaderList![0].Name.Should().Be("Content-Type");
            headers.HeaderList[1].Name.Should().Be("Authorization");
        }

        [Fact]
        public void Headers_WithEmptyList_ShouldHaveZeroCount()
        {
            // Arrange
            var headers = new Headers
            {
                HeaderList = new Collection<Header>()
            };

            // Assert
            headers.HeaderList.Should().NotBeNull();
            headers.HeaderList.Should().BeEmpty();
        }

        [Fact]
        public void Headers_WithMultipleHeaders_ShouldMaintainOrder()
        {
            // Arrange
            var headers = new Headers
            {
                HeaderList = new Collection<Header>
                {
                    new Header { Name = "First", Value = "1" },
                    new Header { Name = "Second", Value = "2" },
                    new Header { Name = "Third", Value = "3" }
                }
            };

            // Assert
            headers.HeaderList.Should().HaveCount(3);
            headers.HeaderList![0].Name.Should().Be("First");
            headers.HeaderList[1].Name.Should().Be("Second");
            headers.HeaderList[2].Name.Should().Be("Third");
        }

        [Fact]
        public void Set_WithNullRequest_ShouldNotThrow()
        {
            // Arrange
            var headers = new Headers
            {
                HeaderList = new Collection<Header>
                {
                    new Header { Name = "Test", Value = "value" }
                }
            };

            // Act
            System.Action act = () => headers.Set(null!);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Set_WithNullHeaderList_ShouldNotThrow()
        {
            // Arrange
            var headers = new Headers
            {
                HeaderList = null
            };
            var request = new HttpRequestMessage();

            // Act
            System.Action act = () => headers.Set(request);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Set_WithEmptyHeaderList_ShouldNotThrow()
        {
            // Arrange
            var headers = new Headers
            {
                HeaderList = new Collection<Header>()
            };
            var request = new HttpRequestMessage();

            // Act
            System.Action act = () => headers.Set(request);

            // Assert
            act.Should().NotThrow();
        }
    }
}
