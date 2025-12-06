using FluentAssertions;
using System.Collections.ObjectModel;
using TE.FileWatcher;
using TE.FileWatcher.Configuration;
using Xunit;

namespace FileWatcher.Tests.Configuration
{
    // Concrete implementation for testing abstract RunnableBase
    public class TestRunnable : RunnableBase
    {
        public bool RunWasCalled { get; private set; }
        public ChangeInfo? ReceivedChange { get; private set; }
        public TriggerType ReceivedTrigger { get; private set; }

        public override void Run(ChangeInfo change, TriggerType trigger)
        {
            base.Run(change, trigger);
            RunWasCalled = true;
            ReceivedChange = change;
            ReceivedTrigger = trigger;
        }
    }

    public class RunnableBaseTests
    {
        [Fact]
        public void RunnableBase_ShouldInitializeWithDefaultValues()
        {
            // Act
            var runnable = new TestRunnable();

            // Assert
            runnable.WaitBefore.Should().Be(0);
            runnable.Triggers.Should().NotBeNull();
        }

        [Fact]
        public void RunnableBase_ShouldSetWaitBeforeCorrectly()
        {
            // Arrange & Act
            var runnable = new TestRunnable
            {
                WaitBefore = 1000
            };

            // Assert
            runnable.WaitBefore.Should().Be(1000);
        }

        [Fact]
        public void Run_WithNullChange_ShouldThrowArgumentNullException()
        {
            // Arrange
            var runnable = new TestRunnable
            {
                Triggers = new Triggers
                {
                    TriggerList = new Collection<TriggerType> { TriggerType.Create }
                }
            };

            // Act
            System.Action act = () => runnable.Run(null!, TriggerType.Create);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("change");
        }

        [Fact]
        public void Run_WithNullTriggerList_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var runnable = new TestRunnable
            {
                Triggers = new Triggers
                {
                    TriggerList = null
                }
            };
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act
            System.Action act = () => runnable.Run(change, TriggerType.Create);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("The list of triggers was not provided.");
        }

        [Fact]
        public void Run_WithEmptyTriggerList_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var runnable = new TestRunnable
            {
                Triggers = new Triggers
                {
                    TriggerList = new Collection<TriggerType>()
                }
            };
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act
            System.Action act = () => runnable.Run(change, TriggerType.Create);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("No triggers were defined.");
        }

        [Fact]
        public void Run_WithNonMatchingTrigger_ShouldThrowFileWatcherTriggerNotMatchException()
        {
            // Arrange
            var runnable = new TestRunnable
            {
                Triggers = new Triggers
                {
                    TriggerList = new Collection<TriggerType> { TriggerType.Create }
                }
            };
            var change = new ChangeInfo(TriggerType.Delete, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act
            System.Action act = () => runnable.Run(change, TriggerType.Delete);

            // Assert
            act.Should().Throw<FileWatcherTriggerNotMatchException>()
                .WithMessage("The trigger doesn't match the list of triggers for this watch.");
        }

        [Fact]
        public void Run_WithMatchingTrigger_ShouldSucceed()
        {
            // Arrange
            var runnable = new TestRunnable
            {
                Triggers = new Triggers
                {
                    TriggerList = new Collection<TriggerType> { TriggerType.Create }
                }
            };
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act
            runnable.Run(change, TriggerType.Create);

            // Assert
            runnable.RunWasCalled.Should().BeTrue();
            runnable.ReceivedChange.Should().BeSameAs(change);
            runnable.ReceivedTrigger.Should().Be(TriggerType.Create);
        }

        [Fact]
        public void Run_WithStepTrigger_ShouldBypassTriggerValidation()
        {
            // Arrange
            var runnable = new TestRunnable
            {
                Triggers = new Triggers
                {
                    TriggerList = new Collection<TriggerType> { TriggerType.Create }
                }
            };
            var change = new ChangeInfo(TriggerType.Delete, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act - Using TriggerType.Step should bypass normal trigger validation
            runnable.Run(change, TriggerType.Step);

            // Assert
            runnable.RunWasCalled.Should().BeTrue();
        }

        [Fact]
        public void Run_WithMultipleTriggers_ShouldMatchAny()
        {
            // Arrange
            var runnable = new TestRunnable
            {
                Triggers = new Triggers
                {
                    TriggerList = new Collection<TriggerType>
                    {
                        TriggerType.Create,
                        TriggerType.Change,
                        TriggerType.Delete
                    }
                }
            };
            var change = new ChangeInfo(TriggerType.Change, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act
            runnable.Run(change, TriggerType.Change);

            // Assert
            runnable.RunWasCalled.Should().BeTrue();
        }

        [Fact]
        public void Run_ShouldSetChangeProperty()
        {
            // Arrange
            var runnable = new TestRunnable
            {
                Triggers = new Triggers
                {
                    TriggerList = new Collection<TriggerType> { TriggerType.Create }
                }
            };
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act
            runnable.Run(change, TriggerType.Create);

            // Assert
            runnable.Change.Should().BeSameAs(change);
        }

        [Fact]
        public void OnStarted_ShouldInvokeStartedEvent()
        {
            // Arrange
            var runnable = new TestRunnable();
            var eventRaised = false;
            runnable.Started += (sender, e) => eventRaised = true;
            var eventArgs = new TaskEventArgs(true, "test-id", "Test started");

            // Act
            runnable.OnStarted(this, eventArgs);

            // Assert
            eventRaised.Should().BeTrue();
        }

        [Fact]
        public void OnCompleted_ShouldInvokeCompletedEvent()
        {
            // Arrange
            var runnable = new TestRunnable();
            var eventRaised = false;
            runnable.Completed += (sender, e) => eventRaised = true;
            var eventArgs = new TaskEventArgs(true, "test-id", "Test completed");

            // Act
            runnable.OnCompleted(this, eventArgs);

            // Assert
            eventRaised.Should().BeTrue();
        }
    }
}
