using FluentAssertions;
using TE.FileWatcher.Configuration;
using Xunit;

namespace FileWatcher.Tests.Configuration
{
    public class StepTests
    {
        [Fact]
        public void Step_ShouldInitializeWithEmptyId()
        {
            // Act
            var step = new Step();

            // Assert
            step.Id.Should().NotBeNull();
            step.Id.Should().BeEmpty();
        }

        [Fact]
        public void Step_ShouldInitializeWithNullComponents()
        {
            // Act
            var step = new Step();

            // Assert
            step.Action.Should().BeNull();
            step.Command.Should().BeNull();
            step.Notification.Should().BeNull();
        }

        [Fact]
        public void Step_ShouldSetIdCorrectly()
        {
            // Arrange & Act
            var step = new Step
            {
                Id = "step-1"
            };

            // Assert
            step.Id.Should().Be("step-1");
        }

        [Fact]
        public void Step_ShouldSetActionCorrectly()
        {
            // Arrange
            var action = new TE.FileWatcher.Configuration.Action
            {
                Type = TE.FileWatcher.Configuration.Action.ActionType.Copy
            };

            // Act
            var step = new Step
            {
                Id = "step-1",
                Action = action
            };

            // Assert
            step.Action.Should().NotBeNull();
            step.Action.Should().BeSameAs(action);
        }

        [Fact]
        public void Step_ShouldSetCommandCorrectly()
        {
            // Arrange
            var command = new Command
            {
                Path = "test.exe"
            };

            // Act
            var step = new Step
            {
                Id = "step-1",
                Command = command
            };

            // Assert
            step.Command.Should().NotBeNull();
            step.Command.Should().BeSameAs(command);
        }

        [Fact]
        public void Step_ShouldSetNotificationCorrectly()
        {
            // Arrange
            var notification = new Notification();

            // Act
            var step = new Step
            {
                Id = "step-1",
                Notification = notification
            };

            // Assert
            step.Notification.Should().NotBeNull();
            step.Notification.Should().BeSameAs(notification);
        }

        [Fact]
        public void Step_WithMultipleComponents_ShouldHoldAll()
        {
            // Arrange
            var action = new TE.FileWatcher.Configuration.Action();
            var command = new Command();
            var notification = new Notification();

            // Act
            var step = new Step
            {
                Id = "multi-step",
                Action = action,
                Command = command,
                Notification = notification
            };

            // Assert
            step.Id.Should().Be("multi-step");
            step.Action.Should().BeSameAs(action);
            step.Command.Should().BeSameAs(command);
            step.Notification.Should().BeSameAs(notification);
        }

        [Fact]
        public void Step_WithLongId_ShouldAcceptAnyLength()
        {
            // Arrange
            var longId = new string('a', 1000);

            // Act
            var step = new Step
            {
                Id = longId
            };

            // Assert
            step.Id.Should().HaveLength(1000);
            step.Id.Should().Be(longId);
        }

        [Fact]
        public void Step_WithSpecialCharactersInId_ShouldAcceptThem()
        {
            // Arrange
            var specialId = "step-1_test.v2@prod";

            // Act
            var step = new Step
            {
                Id = specialId
            };

            // Assert
            step.Id.Should().Be(specialId);
        }
    }
}
