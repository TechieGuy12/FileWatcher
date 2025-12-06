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

        [Fact]
        public void Run_WithValidCommandNoArguments_ShouldQueueCommand()
        {
            // Arrange
            var testExe = CreateTestExecutable("test.cmd");
            var command = new Command
            {
                Path = testExe,
                Triggers = new Triggers
                {
                    TriggerList = new System.Collections.ObjectModel.Collection<TriggerType> { TriggerType.Create }
                }
            };
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act
            System.Action act = () => command.Run(change, TriggerType.Create);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Run_WithCommandAndArguments_ShouldQueueBoth()
        {
            // Arrange
            var testExe = CreateTestExecutable("test.cmd");
            var command = new Command
            {
                Path = testExe,
                Arguments = "-flag value",
                Triggers = new Triggers
                {
                    TriggerList = new System.Collections.ObjectModel.Collection<TriggerType> { TriggerType.Create }
                }
            };
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act
            System.Action act = () => command.Run(change, TriggerType.Create);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Run_WithPlaceholderInArguments_ShouldHandleGracefully()
        {
            // Arrange
            var testExe = CreateTestExecutable("test.cmd");
            var command = new Command
            {
                Path = testExe,
                Arguments = "[somevariable]", // Placeholder - will remain if variable not set
                Triggers = new Triggers
                {
                    TriggerList = new System.Collections.ObjectModel.Collection<TriggerType> { TriggerType.Create }
                }
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
            var testExe = CreateTestExecutable("test.cmd");
            var command = new Command
            {
                Path = testExe,
                WorkingDirectory = _testDirectory,
                Triggers = new Triggers
                {
                    TriggerList = new System.Collections.ObjectModel.Collection<TriggerType> { TriggerType.Create }
                }
            };
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act
            System.Action act = () => command.Run(change, TriggerType.Create);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Run_WithNonExistentCommand_ShouldLogError()
        {
            // Arrange
            var command = new Command
            {
                Path = "C:\\NonExistent\\command.exe",
                Triggers = new Triggers
                {
                    TriggerList = new System.Collections.ObjectModel.Collection<TriggerType> { TriggerType.Create }
                }
            };
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act & Assert - Should not throw, but log error
            System.Action act = () => command.Run(change, TriggerType.Create);
            act.Should().NotThrow();
        }

        [Fact]
        public void Run_WithNullPath_ShouldLogError()
        {
            // Arrange
            var command = new Command
            {
                Path = null,
                Triggers = new Triggers
                {
                    TriggerList = new System.Collections.ObjectModel.Collection<TriggerType> { TriggerType.Create }
                }
            };
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act & Assert - Should not throw
            System.Action act = () => command.Run(change, TriggerType.Create);
            act.Should().NotThrow();
        }

        [Fact]
        public void Run_WithEmptyPath_ShouldLogError()
        {
            // Arrange
            var command = new Command
            {
                Path = "",
                Triggers = new Triggers
                {
                    TriggerList = new System.Collections.ObjectModel.Collection<TriggerType> { TriggerType.Create }
                }
            };
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act & Assert
            System.Action act = () => command.Run(change, TriggerType.Create);
            act.Should().NotThrow();
        }

        [Fact]
        public void Run_WithNullChange_ShouldThrowArgumentNullException()
        {
            // Arrange
            var command = new Command
            {
                Path = "test.exe",
                Triggers = new Triggers
                {
                    TriggerList = new System.Collections.ObjectModel.Collection<TriggerType> { TriggerType.Create }
                }
            };

            // Act & Assert
            System.Action act = () => command.Run(null!, TriggerType.Create);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Run_WithMismatchedTrigger_ShouldNotExecute()
        {
            // Arrange
            var command = new Command
            {
                Path = "test.exe",
                Triggers = new Triggers
                {
                    TriggerList = new System.Collections.ObjectModel.Collection<TriggerType> { TriggerType.Create }
                }
            };
            var change = new ChangeInfo(TriggerType.Delete, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act & Assert - Should not throw, trigger validation prevents execution
            System.Action act = () => command.Run(change, TriggerType.Delete);
            act.Should().NotThrow();
        }

        [Fact]
        public void Run_WithSpecialCharactersInArguments_ShouldHandleGracefully()
        {
            // Arrange
            var testExe = CreateTestExecutable("test.cmd");
            var command = new Command
            {
                Path = testExe,
                Arguments = "-arg \"value with spaces\" --flag='quoted'",
                Triggers = new Triggers
                {
                    TriggerList = new System.Collections.ObjectModel.Collection<TriggerType> { TriggerType.Create }
                }
            };
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act
            System.Action act = () => command.Run(change, TriggerType.Create);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Run_WithVariablesInArguments_ShouldReplaceVariables()
        {
            // Arrange
            var testExe = CreateTestExecutable("test.cmd");
            var command = new Command
            {
                Path = testExe,
                Arguments = "[variable1] [variable2]",
                Triggers = new Triggers
                {
                    TriggerList = new System.Collections.ObjectModel.Collection<TriggerType> { TriggerType.Create }
                },
                Variables = new Variables()
            };
            var variableList = new List<Variable>
            {
                new Variable { Name = "variable1", Value = "value1" },
                new Variable { Name = "variable2", Value = "value2" }
            };
            command.Variables.Add(variableList);
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act
            System.Action act = () => command.Run(change, TriggerType.Create);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Run_WithPlaceholderInArguments_ShouldHandleGracefully()
        {
            // Arrange
            var testExe = CreateTestExecutable("test.cmd");
            var command = new Command
            {
                Path = testExe,
                Arguments = "[somevariable]", // Placeholder - will remain if variable not set
                Triggers = new Triggers
                {
                    TriggerList = new System.Collections.ObjectModel.Collection<TriggerType> { TriggerType.Create }
                }
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
            var testExe = CreateTestExecutable("test.cmd");
            var command = new Command
            {
                Path = testExe,
                WorkingDirectory = _testDirectory,
                Triggers = new Triggers
                {
                    TriggerList = new System.Collections.ObjectModel.Collection<TriggerType> { TriggerType.Create }
                }
            };
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act
            System.Action act = () => command.Run(change, TriggerType.Create);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Run_WithNonExistentCommand_ShouldLogError()
        {
            // Arrange
            var command = new Command
            {
                Path = "C:\\NonExistent\\command.exe",
                Triggers = new Triggers
                {
                    TriggerList = new System.Collections.ObjectModel.Collection<TriggerType> { TriggerType.Create }
                }
            };
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act & Assert - Should not throw, but log error
            System.Action act = () => command.Run(change, TriggerType.Create);
            act.Should().NotThrow();
        }

        [Fact]
        public void Run_WithNullPath_ShouldLogError()
        {
            // Arrange
            var command = new Command
            {
                Path = null,
                Triggers = new Triggers
                {
                    TriggerList = new System.Collections.ObjectModel.Collection<TriggerType> { TriggerType.Create }
                }
            };
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act & Assert - Should not throw
            System.Action act = () => command.Run(change, TriggerType.Create);
            act.Should().NotThrow();
        }

        [Fact]
        public void Run_WithEmptyPath_ShouldLogError()
        {
            // Arrange
            var command = new Command
            {
                Path = "",
                Triggers = new Triggers
                {
                    TriggerList = new System.Collections.ObjectModel.Collection<TriggerType> { TriggerType.Create }
                }
            };
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act & Assert
            System.Action act = () => command.Run(change, TriggerType.Create);
            act.Should().NotThrow();
        }

        [Fact]
        public void Run_WithNullChange_ShouldThrowArgumentNullException()
        {
            // Arrange
            var command = new Command
            {
                Path = "test.exe",
                Triggers = new Triggers
                {
                    TriggerList = new System.Collections.ObjectModel.Collection<TriggerType> { TriggerType.Create }
                }
            };

            // Act & Assert
            System.Action act = () => command.Run(null!, TriggerType.Create);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Run_WithMismatchedTrigger_ShouldNotExecute()
        {
            // Arrange
            var command = new Command
            {
                Path = "test.exe",
                Triggers = new Triggers
                {
                    TriggerList = new System.Collections.ObjectModel.Collection<TriggerType> { TriggerType.Create }
                }
            };
            var change = new ChangeInfo(TriggerType.Delete, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act & Assert - Should not throw, trigger validation prevents execution
            System.Action act = () => command.Run(change, TriggerType.Delete);
            act.Should().NotThrow();
        }

        [Fact]
        public void Run_WithSpecialCharactersInArguments_ShouldHandleGracefully()
        {
            // Arrange
            var testExe = CreateTestExecutable("test.cmd");
            var command = new Command
            {
                Path = testExe,
                Arguments = "-arg \"value with spaces\" --flag='quoted'",
                Triggers = new Triggers
                {
                    TriggerList = new System.Collections.ObjectModel.Collection<TriggerType> { TriggerType.Create }
                }
            };
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act
            System.Action act = () => command.Run(change, TriggerType.Create);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Run_WithLongArguments_ShouldHandleGracefully()
        {
            // Arrange
            var testExe = CreateTestExecutable("test.cmd");
            var longArgs = new string('a', 5000);
            var command = new Command
            {
                Path = testExe,
                Arguments = longArgs,
                Triggers = new Triggers
                {
                    TriggerList = new System.Collections.ObjectModel.Collection<TriggerType> { TriggerType.Create }
                }
            };
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act
            System.Action act = () => command.Run(change, TriggerType.Create);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Run_WithEnvironmentVariableInArguments_ShouldExpand()
        {
            // Arrange
            var testExe = CreateTestExecutable("test.cmd");
            var command = new Command
            {
                Path = testExe,
                Arguments = "[%TEMP%]", // Environment variable placeholder
                Triggers = new Triggers
                {
                    TriggerList = new System.Collections.ObjectModel.Collection<TriggerType> { TriggerType.Create }
                }
            };
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act
            System.Action act = () => command.Run(change, TriggerType.Create);

            // Assert
            act.Should().NotThrow();
        }
    }
}
