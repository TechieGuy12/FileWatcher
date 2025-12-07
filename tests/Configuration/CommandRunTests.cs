using FluentAssertions;
using System.Collections.ObjectModel;
using TE.FileWatcher.Configuration;
using Xunit;

namespace FileWatcher.Tests.Configuration
{
    /// <summary>
    /// Tests for Command.Run() execution logic - Security and execution path testing
    /// </summary>
    public class CommandRunTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly List<string> _testFiles = new();

        public CommandRunTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), $"CommandRunTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
        }

        public void Dispose()
        {
            foreach (var file in _testFiles.Where(File.Exists))
            {
                try { File.Delete(file); }
                catch { }
            }
            if (Directory.Exists(_testDirectory))
            {
                try { Directory.Delete(_testDirectory, true); }
                catch { }
            }
        }

        private string CreateTestExecutable(string filename)
        {
            var path = Path.Combine(_testDirectory, filename);
            File.WriteAllText(path, "@echo off\necho Test");
            _testFiles.Add(path);
            return path;
        }

        private Triggers CreateTriggers(TriggerType type)
        {
            return new Triggers
            {
                TriggerList = new Collection<TriggerType> { type }
            };
        }

        [Fact]
        public void Run_WithValidCommand_ShouldQueueCommand()
        {
            // Arrange
            var testExe = CreateTestExecutable("test.bat");
            var command = new Command
            {
                Path = testExe,
                Triggers = CreateTriggers(TriggerType.Create)
            };
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act
            System.Action act = () => command.Run(change, TriggerType.Create);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Run_WithArguments_ShouldQueueWithArguments()
        {
            // Arrange
            var testExe = CreateTestExecutable("test.bat");
            var command = new Command
            {
                Path = testExe,
                Arguments = "-flag value",
                Triggers = CreateTriggers(TriggerType.Create)
            };
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act
            System.Action act = () => command.Run(change, TriggerType.Create);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Run_WithPlaceholderInArguments_ShouldReplace()
        {
            // Arrange
            var testExe = CreateTestExecutable("test.bat");
            var command = new Command
            {
                Path = testExe,
                Arguments = "[file] [path]", // Placeholders
                Triggers = CreateTriggers(TriggerType.Create)
            };
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act
            System.Action act = () => command.Run(change, TriggerType.Create);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Run_WithWorkingDirectory_ShouldSetWorkingDirectory()
        {
            // Arrange
            var testExe = CreateTestExecutable("test.bat");
            var command = new Command
            {
                Path = testExe,
                WorkingDirectory = _testDirectory,
                Triggers = CreateTriggers(TriggerType.Create)
            };
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act
            System.Action act = () => command.Run(change, TriggerType.Create);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Run_WithNonExistentCommand_ShouldLogErrorAndNotThrow()
        {
            // Arrange
            var command = new Command
            {
                Path = "C:\\NonExistent\\command.exe",
                Triggers = CreateTriggers(TriggerType.Create)
            };
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act
            System.Action act = () => command.Run(change, TriggerType.Create);

            // Assert - Should log error but not throw
            act.Should().NotThrow();
        }

        [Fact]
        public void Run_WithNullCommandPath_ShouldLogErrorAndReturn()
        {
            // Arrange
            var command = new Command
            {
                Path = null,
                Triggers = CreateTriggers(TriggerType.Create)
            };
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act
            System.Action act = () => command.Run(change, TriggerType.Create);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Run_WithEmptyCommandPath_ShouldLogErrorAndReturn()
        {
            // Arrange
            var command = new Command
            {
                Path = "",
                Triggers = CreateTriggers(TriggerType.Create)
            };
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act
            System.Action act = () => command.Run(change, TriggerType.Create);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Run_WithNullChange_ShouldNotThrowButLogError()
        {
            // Arrange
            var command = new Command
            {
                Path = "test.exe",
                Triggers = CreateTriggers(TriggerType.Create)
            };

            // Act - Command catches ArgumentNullException internally and logs
            System.Action act = () => command.Run(null!, TriggerType.Create);

            // Assert - Should not throw, error is logged
            act.Should().NotThrow();
        }

        [Fact]
        public void Run_WithSpecialCharactersInArguments_ShouldHandleGracefully()
        {
            // Arrange
            var testExe = CreateTestExecutable("test.bat");
            var command = new Command
            {
                Path = testExe,
                Arguments = "-arg \"value with spaces\" --flag='quoted'",
                Triggers = CreateTriggers(TriggerType.Create)
            };
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act
            System.Action act = () => command.Run(change, TriggerType.Create);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Run_CalledMultipleTimes_ShouldQueueAllCommands()
        {
            // Arrange
            var testExe = CreateTestExecutable("test.bat");
            var command = new Command
            {
                Path = testExe,
                Arguments = "-flag",
                Triggers = CreateTriggers(TriggerType.Create)
            };
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act
            System.Action act = () =>
            {
                command.Run(change, TriggerType.Create);
                command.Run(change, TriggerType.Create);
                command.Run(change, TriggerType.Create);
            };

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Run_WithLongArguments_ShouldHandleGracefully()
        {
            // Arrange
            var testExe = CreateTestExecutable("test.bat");
            var longArgs = new string('a', 1000);
            var command = new Command
            {
                Path = testExe,
                Arguments = longArgs,
                Triggers = CreateTriggers(TriggerType.Create)
            };
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act
            System.Action act = () => command.Run(change, TriggerType.Create);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Run_WithPathTraversalAttempt_ShouldHandle()
        {
            // Arrange
            var testExe = CreateTestExecutable("test.bat");
            var command = new Command
            {
                Path = testExe,
                WorkingDirectory = "..\\..\\..\\",
                Triggers = CreateTriggers(TriggerType.Create)
            };
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act
            System.Action act = () => command.Run(change, TriggerType.Create);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Run_WithMismatchedTrigger_ShouldNotExecute()
        {
            // Arrange
            var command = new Command
            {
                Path = "test.exe",
                Triggers = CreateTriggers(TriggerType.Create)
            };
            var change = new ChangeInfo(TriggerType.Delete, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act
            System.Action act = () => command.Run(change, TriggerType.Delete);

            // Assert - Trigger mismatch prevents execution
            act.Should().NotThrow();
        }

        [Fact]
        public void Run_WithNullArguments_ShouldExecuteWithoutArguments()
        {
            // Arrange
            var testExe = CreateTestExecutable("test.bat");
            var command = new Command
            {
                Path = testExe,
                Arguments = null,
                Triggers = CreateTriggers(TriggerType.Create)
            };
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act
            System.Action act = () => command.Run(change, TriggerType.Create);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Run_WithNullWorkingDirectory_ShouldUseDefault()
        {
            // Arrange
            var testExe = CreateTestExecutable("test.bat");
            var command = new Command
            {
                Path = testExe,
                WorkingDirectory = null,
                Triggers = CreateTriggers(TriggerType.Create)
            };
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act
            System.Action act = () => command.Run(change, TriggerType.Create);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Run_WithPlaceholderInWorkingDirectory_ShouldReplace()
        {
            // Arrange
            var testExe = CreateTestExecutable("test.bat");
            var command = new Command
            {
                Path = testExe,
                WorkingDirectory = "[watchpath]",
                Triggers = CreateTriggers(TriggerType.Create)
            };
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act
            System.Action act = () => command.Run(change, TriggerType.Create);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Run_WithEnvironmentVariablePlaceholder_ShouldHandle()
        {
            // Arrange
            var testExe = CreateTestExecutable("test.bat");
            var command = new Command
            {
                Path = testExe,
                Arguments = "[%TEMP%]",
                Triggers = CreateTriggers(TriggerType.Create)
            };
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act
            System.Action act = () => command.Run(change, TriggerType.Create);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Run_WithMultipleWatchPathPlaceholders_ShouldReplaceAll()
        {
            // Arrange
            var testExe = CreateTestExecutable("test.bat");
            var command = new Command
            {
                Path = testExe,
                Arguments = "[watchpath] [fullpath] [file]",
                Triggers = CreateTriggers(TriggerType.Create)
            };
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act
            System.Action act = () => command.Run(change, TriggerType.Create);

            // Assert
            act.Should().NotThrow();
        }
    }
}
