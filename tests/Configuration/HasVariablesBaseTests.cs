using FluentAssertions;
using TE.FileWatcher.Configuration;
using Xunit;

namespace FileWatcher.Tests.Configuration
{
    // Concrete implementation for testing HasVariablesBase
    public class TestHasVariables : HasVariablesBase
    {
    }

    public class HasVariablesBaseTests
    {
        [Fact]
        public void HasVariablesBase_ShouldInitializeWithNullVariables()
        {
            // Act
            var obj = new TestHasVariables();

            // Assert
            obj.Variables.Should().BeNull();
        }

        [Fact]
        public void HasVariablesBase_ShouldSetVariables()
        {
            // Arrange
            var obj = new TestHasVariables();
            var variables = new Variables();

            // Act
            obj.Variables = variables;

            // Assert
            obj.Variables.Should().NotBeNull();
            obj.Variables.Should().BeSameAs(variables);
        }

        [Fact]
        public void HasVariablesBase_CanSetVariablesToNull()
        {
            // Arrange
            var obj = new TestHasVariables
            {
                Variables = new Variables()
            };

            // Act
            obj.Variables = null;

            // Assert
            obj.Variables.Should().BeNull();
        }

        [Fact]
        public void HasVariablesBase_WithVariables_ShouldRetainReference()
        {
            // Arrange
            var obj = new TestHasVariables();
            var variables = new Variables();

            // Act
            obj.Variables = variables;
            var retrieved = obj.Variables;

            // Assert
            retrieved.Should().BeSameAs(variables);
        }

        [Fact]
        public void HasVariablesBase_CanReplaceVariables()
        {
            // Arrange
            var obj = new TestHasVariables
            {
                Variables = new Variables()
            };
            var newVariables = new Variables();

            // Act
            obj.Variables = newVariables;

            // Assert
            obj.Variables.Should().BeSameAs(newVariables);
        }
    }
}
