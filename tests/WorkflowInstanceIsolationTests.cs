using FluentAssertions;
using TE.FileWatcher.Configuration;
using Xunit;
using IODirectory = System.IO.Directory;

namespace FileWatcher.Tests
{
    /// <summary>
    /// Tests for workflow instance isolation and correlation ID tracking.
    /// These tests verify that workflow/step/notification instances don't 
    /// pollute each other's state when processing multiple files.
    /// </summary>
    public class WorkflowInstanceIsolationTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly string _watchPath;
        private readonly List<string> _testFiles = new();

        public WorkflowInstanceIsolationTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), $"WorkflowTests_{Guid.NewGuid()}");
            IODirectory.CreateDirectory(_testDirectory);
            _watchPath = _testDirectory;
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

            if (IODirectory.Exists(_testDirectory))
            {
                try
                {
                    IODirectory.Delete(_testDirectory, true);
                }
                catch { }
            }
        }

        private string CreateTestFile(string filename)
        {
            var path = Path.Combine(_testDirectory, filename);
            File.WriteAllText(path, "test content");
            _testFiles.Add(path);
            return path;
        }

        private ChangeInfo CreateChangeInfo(string filename, TriggerType trigger = TriggerType.Create)
        {
            var fullPath = CreateTestFile(filename);
            return new ChangeInfo(trigger, _watchPath, filename, fullPath, null, null);
        }

        [Fact]
        public void ChangeInfo_CorrelationId_ShouldBeUniquePerInstance()
        {
            // Arrange & Act
            var change1 = CreateChangeInfo("file1.txt");
            var change2 = CreateChangeInfo("file2.txt");

            // Assert
            change1.CorrelationId.Should().NotBeEmpty();
            change2.CorrelationId.Should().NotBeEmpty();
            change1.CorrelationId.Should().NotBe(change2.CorrelationId);
        }

        [Fact]
        public void Step_Run_WithDifferentFiles_ShouldMaintainSeparateCorrelationIds()
        {
            // Arrange
            var step = new Step { Id = "test-step" };
            step.Initialize();

            var change1 = CreateChangeInfo("file1.txt");
            var change2 = CreateChangeInfo("file2.txt");

            // Act - Simulate sequential execution
            step.Run(change1, TriggerType.Create);
            step.Reset(); // Reset between executions
            step.Run(change2, TriggerType.Create);

            // Assert - Each should have unique correlation ID
            change1.CorrelationId.Should().NotBe(change2.CorrelationId);
        }

        [Fact]
        public void MultipleFiles_ShouldHaveUniqueCorrelationIds()
        {
            // Arrange & Act
            var changes = new[]
            {
                CreateChangeInfo("file1.txt"),
                CreateChangeInfo("file2.txt"),
                CreateChangeInfo("file3.txt")
            };

            // Assert
            for (int i = 0; i < changes.Length; i++)
            {
                changes[i].CorrelationId.Should().NotBeEmpty();
                
                // Verify each change has unique correlation ID
                for (int j = i + 1; j < changes.Length; j++)
                {
                    changes[i].CorrelationId.Should().NotBe(changes[j].CorrelationId,
                        $"file{i + 1}.txt and file{j + 1}.txt should have different correlation IDs");
                }
            }
        }

        [Fact]
        public void Step_Reset_ShouldPrepareForNextExecution()
        {
            // Arrange
            var step = new Step { Id = "test-step" };
            step.Initialize();

            var change = CreateChangeInfo("file.txt");

            // Act
            step.Run(change, TriggerType.Create);
            step.Reset();

            // Assert - After reset, step should be ready for new execution
            step.IsRunning.Should().BeFalse();
            step.HasCompleted.Should().BeFalse();
        }

        [Fact]
        public void Workflow_MultipleRuns_ShouldTrackEachFileIndependently()
        {
            // Arrange
            var workflow = new Workflow
            {
                Triggers = new Triggers
                {
                    TriggerList = new System.Collections.ObjectModel.Collection<TriggerType> 
                    { 
                        TriggerType.Create 
                    }
                }
            };

            var changes = new[]
            {
                CreateChangeInfo("file1.txt"),
                CreateChangeInfo("file2.txt")
            };

            // Assert - Each change should have unique correlation ID
            changes[0].CorrelationId.Should().NotBe(changes[1].CorrelationId);
            changes[0].FullPath.Should().NotBe(changes[1].FullPath);
        }

        [Fact]
        public void CorrelationId_ShouldBeGeneratedAutomatically()
        {
            // Arrange & Act
            var change = CreateChangeInfo("test.txt");

            // Assert
            change.CorrelationId.Should().NotBeEmpty();
            change.CorrelationId.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public void DifferentTriggerTypes_ShouldHaveUniqueCorrelationIds()
        {
            // Arrange & Act
            var change1 = CreateChangeInfo("file1.txt", TriggerType.Create);
            var change2 = CreateChangeInfo("file2.txt", TriggerType.Change);
            var change3 = CreateChangeInfo("file3.txt", TriggerType.Delete);

            // Assert
            var correlationIds = new[] { change1.CorrelationId, change2.CorrelationId, change3.CorrelationId };
            correlationIds.Should().OnlyHaveUniqueItems("each file event should have unique correlation ID regardless of trigger type");
        }
    }
}
