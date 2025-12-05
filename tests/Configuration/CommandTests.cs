using FluentAssertions;
using System.Collections.Concurrent;
using TE.FileWatcher.Configuration;
using Xunit;

namespace FileWatcher.Tests.Configuration
{
    public class CommandTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly List<string> _testFiles = new();

        public CommandTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), $"CommandTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
        }

        public void Dispose()
        {
            foreach (var file in _testFiles.Where(File.Exists))
            {
                try
                {
                    File.Delete(file);
                }
                catch { }
            }

            if (Directory.Exists(_testDirectory))
            {
                try
                {
                    Directory.Delete(_testDirectory, true);
                }
                catch { }
            }
        }

        private string CreateTestExecutable(string filename)
        {
            var path = Path.Combine(_testDirectory, filename);
            File.WriteAllText(path, "test executable");
            _testFiles.Add(path);
            return path;
        }

        [Fact]
        public void Command_ShouldHaveNullPropertiesByDefault()
        {
            // Act
            var command = new Command();

            // Assert
            command.Arguments.Should().BeNull();
            command.Path.Should().BeNull();
            command.WorkingDirectory.Should().BeNull();
        }

        [Fact]
        public void Command_ShouldSetPropertiesCorrectly()
        {
            // Arrange & Act
            var command = new Command
            {
                Path = "test.exe",
                Arguments = "-arg1 -arg2",
                WorkingDirectory = "C:\\temp"
            };

            // Assert
            command.Path.Should().Be("test.exe");
            command.Arguments.Should().Be("-arg1 -arg2");
            command.WorkingDirectory.Should().Be("C:\\temp");
        }

        [Fact]
        public void Dispose_ShouldNotThrowWhenCalledMultipleTimes()
        {
            // Arrange
            var command = new Command();

            // Act
            System.Action act = () =>
            {
                command.Dispose();
                command.Dispose();
                command.Dispose();
            };

            // Assert
            act.Should().NotThrow();
        }
    }
}
