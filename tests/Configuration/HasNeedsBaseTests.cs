using FluentAssertions;
using TE.FileWatcher.Configuration;
using Xunit;

namespace FileWatcher.Tests.Configuration
{
    // Concrete implementation for testing HasNeedsBase
    public class TestHasNeeds : HasNeedsBase
    {
        public bool RunWasCalled { get; private set; }

        public override void Run(ChangeInfo change, TriggerType trigger)
        {
            RunWasCalled = true;
            IsRunning = true;
            HasCompleted = true;
            IsRunning = false;
        }
    }

    public class HasNeedsBaseTests
    {
        [Fact]
        public void HasNeedsBase_ShouldInitializeWithDefaults()
        {
            // Act
            var obj = new TestHasNeeds();

            // Assert
            obj.Needs.Should().BeNull();
            obj.HasCompleted.Should().BeFalse();
            obj.IsInitialized.Should().BeFalse();
            obj.IsRunning.Should().BeFalse();
        }

        [Fact]
        public void HasNeedsBase_CanRun_WithNoNeeds_ShouldReturnTrue()
        {
            // Arrange
            var obj = new TestHasNeeds();

            // Act & Assert
            obj.CanRun.Should().BeTrue();
        }

        [Fact]
        public void Initialize_ShouldSetIsInitializedToTrue()
        {
            // Arrange
            var obj = new TestHasNeeds();

            // Act
            obj.Initialize();

            // Assert
            obj.IsInitialized.Should().BeTrue();
        }

        [Fact]
        public void Initialize_ShouldSetHasCompletedToFalse()
        {
            // Arrange
            var obj = new TestHasNeeds();

            // Act
            obj.Initialize();

            // Assert
            obj.HasCompleted.Should().BeFalse();
        }

        [Fact]
        public void Initialize_ShouldSetIsRunningToFalse()
        {
            // Arrange
            var obj = new TestHasNeeds();

            // Act
            obj.Initialize();

            // Assert
            obj.IsRunning.Should().BeFalse();
        }

        [Fact]
        public void Reset_ShouldCallInitialize()
        {
            // Arrange
            var obj = new TestHasNeeds();

            // Act
            obj.Reset();

            // Assert
            obj.IsInitialized.Should().BeTrue();
            obj.HasCompleted.Should().BeFalse();
            obj.IsRunning.Should().BeFalse();
        }

        [Fact]
        public void Run_ShouldSetHasCompletedToTrue()
        {
            // Arrange
            var obj = new TestHasNeeds();
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act
            obj.Run(change, TriggerType.Create);

            // Assert
            obj.HasCompleted.Should().BeTrue();
        }

        [Fact]
        public void HasNeedsBase_ShouldSetNeeds()
        {
            // Arrange
            var obj = new TestHasNeeds();
            var needs = new[] { "step1", "step2" };

            // Act
            obj.Needs = needs;

            // Assert
            obj.Needs.Should().NotBeNull();
            obj.Needs.Should().HaveCount(2);
            obj.Needs.Should().Contain("step1");
            obj.Needs.Should().Contain("step2");
        }

        [Fact]
        public void HasNeedsBase_WithEmptyNeeds_ShouldAcceptIt()
        {
            // Arrange
            var obj = new TestHasNeeds();

            // Act
            obj.Needs = Array.Empty<string>();

            // Assert
            obj.Needs.Should().NotBeNull();
            obj.Needs.Should().BeEmpty();
        }

        [Fact]
        public void Initialize_CalledMultipleTimes_ShouldRemainInitialized()
        {
            // Arrange
            var obj = new TestHasNeeds();

            // Act
            obj.Initialize();
            obj.Initialize();
            obj.Initialize();

            // Assert
            obj.IsInitialized.Should().BeTrue();
        }

        [Fact]
        public void HasNeedsBase_Started_EventCanBeSubscribed()
        {
            // Arrange
            var obj = new TestHasNeeds();
            var eventFired = false;

            // Act & Assert - Should not throw
            System.Action act = () => obj.Started += (sender, e) => eventFired = true;
            act.Should().NotThrow();
        }

        [Fact]
        public void HasNeedsBase_Completed_EventCanBeSubscribed()
        {
            // Arrange
            var obj = new TestHasNeeds();
            var eventFired = false;

            // Act & Assert - Should not throw
            System.Action act = () => obj.Completed += (sender, e) => eventFired = true;
            act.Should().NotThrow();
        }

        [Fact]
        public void HasNeedsBase_CanSetVariables()
        {
            // Arrange
            var obj = new TestHasNeeds();
            var variables = new Variables();

            // Act
            obj.Variables = variables;

            // Assert
            obj.Variables.Should().BeSameAs(variables);
        }

        [Fact]
        public void Reset_AfterRun_ShouldResetState()
        {
            // Arrange
            var obj = new TestHasNeeds();
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);
            obj.Run(change, TriggerType.Create);

            // Act
            obj.Reset();

            // Assert
            obj.HasCompleted.Should().BeFalse();
            obj.IsRunning.Should().BeFalse();
            obj.IsInitialized.Should().BeTrue();
        }
    }
}
