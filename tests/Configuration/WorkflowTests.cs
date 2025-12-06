using FluentAssertions;
using TE.FileWatcher.Configuration;
using Xunit;

namespace FileWatcher.Tests.Configuration
{
    public class WorkflowTests
    {
        [Fact]
        public void Workflow_ShouldInitializeWithNullSteps()
        {
            // Act
            var workflow = new Workflow();

            // Assert
            workflow.Steps.Should().BeNull();
        }

        [Fact]
        public void Workflow_ShouldInitializeWithNotCompleted()
        {
            // Act
            var workflow = new Workflow();

            // Assert
            workflow.HasCompleted.Should().BeFalse();
        }

        [Fact]
        public void Workflow_ShouldNotBeInitializedByDefault()
        {
            // Act
            var workflow = new Workflow();

            // Assert
            workflow.IsInitialized.Should().BeFalse();
        }

        [Fact]
        public void Workflow_ShouldSetStepsCorrectly()
        {
            // Arrange
            var steps = new Steps();

            // Act
            var workflow = new Workflow
            {
                Steps = steps
            };

            // Assert
            workflow.Steps.Should().NotBeNull();
            workflow.Steps.Should().BeSameAs(steps);
        }

        [Fact]
        public void Initialize_ShouldSetIsInitializedToTrue()
        {
            // Arrange
            var workflow = new Workflow();

            // Act
            workflow.Initialize();

            // Assert
            workflow.IsInitialized.Should().BeTrue();
        }

        [Fact]
        public void Initialize_ShouldSetHasCompletedToFalse()
        {
            // Arrange
            var workflow = new Workflow();

            // Act
            workflow.Initialize();

            // Assert
            workflow.HasCompleted.Should().BeFalse();
        }

        [Fact]
        public void Initialize_WithNullSteps_ShouldNotThrow()
        {
            // Arrange
            var workflow = new Workflow
            {
                Steps = null
            };

            // Act
            System.Action act = () => workflow.Initialize();

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Initialize_WithSteps_ShouldNotThrow()
        {
            // Arrange
            var workflow = new Workflow
            {
                Steps = new Steps()
            };

            // Act
            System.Action act = () => workflow.Initialize();

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Initialize_CalledMultipleTimes_ShouldRemainInitialized()
        {
            // Arrange
            var workflow = new Workflow();

            // Act
            workflow.Initialize();
            workflow.Initialize();
            workflow.Initialize();

            // Assert
            workflow.IsInitialized.Should().BeTrue();
        }
    }
}
