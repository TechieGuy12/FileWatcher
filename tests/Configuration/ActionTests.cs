using FluentAssertions;
using TE.FileWatcher.Configuration;
using Xunit;
using IOFile = System.IO.File;
using IODirectory = System.IO.Directory;

namespace FileWatcher.Tests.Configuration
{
    public class ActionTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly List<string> _testFiles = new();

        public ActionTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), $"ActionTests_{Guid.NewGuid()}");
            IODirectory.CreateDirectory(_testDirectory);
        }

        public void Dispose()
        {
            foreach (var file in _testFiles.Where(IOFile.Exists))
            {
                try
                {
                    IOFile.Delete(file);
                }
                catch { }
            }

            if (IODirectory.Exists(_testDirectory))
            {
                try
                {
                    IODirectory.Delete(_testDirectory, true);
                }
                catch { }
            }
        }

        private string CreateTestFile(string filename, string content = "test content")
        {
            var path = Path.Combine(_testDirectory, filename);
            IOFile.WriteAllText(path, content);
            _testFiles.Add(path);
            return path;
        }

        [Fact]
        public void Action_ShouldHaveDefaultProperties()
        {
            // Act
            var action = new TE.FileWatcher.Configuration.Action();

            // Assert
            action.Type.Should().Be(TE.FileWatcher.Configuration.Action.ActionType.Copy);
            action.Source.Should().Be("[fullpath]"); // Default placeholder
            action.Destination.Should().BeNull();
            action.Verify.Should().BeFalse();
            action.KeepTimestamps.Should().BeFalse();
        }

        [Fact]
        public void Action_ShouldSetTypeCorrectly()
        {
            // Arrange & Act
            var action = new TE.FileWatcher.Configuration.Action
            {
                Type = TE.FileWatcher.Configuration.Action.ActionType.Move
            };

            // Assert
            action.Type.Should().Be(TE.FileWatcher.Configuration.Action.ActionType.Move);
        }

        [Fact]
        public void Action_ShouldSetSourceCorrectly()
        {
            // Arrange & Act
            var action = new TE.FileWatcher.Configuration.Action
            {
                Source = "C:\\source\\file.txt"
            };

            // Assert
            action.Source.Should().Be("C:\\source\\file.txt");
        }

        [Fact]
        public void Action_ShouldSetDestinationCorrectly()
        {
            // Arrange & Act
            var action = new TE.FileWatcher.Configuration.Action
            {
                Destination = "C:\\dest\\file.txt"
            };

            // Assert
            action.Destination.Should().Be("C:\\dest\\file.txt");
        }

        [Fact]
        public void Action_ShouldSetVerifyFlag()
        {
            // Arrange & Act
            var action = new TE.FileWatcher.Configuration.Action
            {
                Verify = true
            };

            // Assert
            action.Verify.Should().BeTrue();
        }

        [Fact]
        public void Action_ShouldSetKeepTimestampsFlag()
        {
            // Arrange & Act
            var action = new TE.FileWatcher.Configuration.Action
            {
                KeepTimestamps = true
            };

            // Assert
            action.KeepTimestamps.Should().BeTrue();
        }

        [Theory]
        [InlineData(TE.FileWatcher.Configuration.Action.ActionType.Copy)]
        [InlineData(TE.FileWatcher.Configuration.Action.ActionType.Move)]
        [InlineData(TE.FileWatcher.Configuration.Action.ActionType.Delete)]
        public void Action_ShouldSupportAllActionTypes(TE.FileWatcher.Configuration.Action.ActionType type)
        {
            // Arrange & Act
            var action = new TE.FileWatcher.Configuration.Action
            {
                Type = type
            };

            // Assert
            action.Type.Should().Be(type);
        }

        [Fact]
        public void Action_WithAllPropertiesSet_ShouldRetainValues()
        {
            // Arrange & Act
            var action = new TE.FileWatcher.Configuration.Action
            {
                Type = TE.FileWatcher.Configuration.Action.ActionType.Move,
                Source = "source.txt",
                Destination = "dest.txt",
                Verify = true,
                KeepTimestamps = true
            };

            // Assert
            action.Type.Should().Be(TE.FileWatcher.Configuration.Action.ActionType.Move);
            action.Source.Should().Be("source.txt");
            action.Destination.Should().Be("dest.txt");
            action.Verify.Should().BeTrue();
            action.KeepTimestamps.Should().BeTrue();
        }
    }
}
