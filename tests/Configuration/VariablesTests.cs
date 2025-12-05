using FluentAssertions;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using TE.FileWatcher.Configuration;
using Xunit;

namespace FileWatcher.Tests.Configuration
{
    public class VariablesTests
    {
        [Fact]
        public void Constructor_ShouldInitializeWithVariablesNotSet()
        {
            // Act
            var variables = new Variables();

            // Assert
            variables.VariablesSet.Should().BeFalse();
            variables.AllVariables.Should().BeNull();
        }

        [Fact]
        public void Add_WithVariableList_ShouldAddToAllVariables()
        {
            // Arrange
            var variables = new Variables
            {
                VariableList = new Collection<Variable>
                {
                    new Variable { Name = "var1", Value = "value1" },
                    new Variable { Name = "var2", Value = "value2" }
                }
            };

            // Act
            variables.Add(null);

            // Assert
            variables.AllVariables.Should().NotBeNull();
            variables.AllVariables.Should().ContainKey("var1");
            variables.AllVariables.Should().ContainKey("var2");
            variables.AllVariables!["var1"].Should().Be("value1");
            variables.AllVariables["var2"].Should().Be("value2");
        }

        [Fact]
        public void Add_WithExternalVariables_ShouldAddToAllVariables()
        {
            // Arrange
            var variables = new Variables();
            var externalVars = new ConcurrentDictionary<string, string>();
            externalVars.TryAdd("ext1", "extvalue1");
            externalVars.TryAdd("ext2", "extvalue2");

            // Act
            variables.Add(externalVars);

            // Assert
            variables.AllVariables.Should().NotBeNull();
            variables.AllVariables.Should().ContainKey("ext1");
            variables.AllVariables.Should().ContainKey("ext2");
            variables.AllVariables!["ext1"].Should().Be("extvalue1");
            variables.AllVariables["ext2"].Should().Be("extvalue2");
        }

        [Fact]
        public void Add_WithBothVariableListAndExternal_ShouldCombineBoth()
        {
            // Arrange
            var variables = new Variables
            {
                VariableList = new Collection<Variable>
                {
                    new Variable { Name = "var1", Value = "value1" }
                }
            };
            var externalVars = new ConcurrentDictionary<string, string>();
            externalVars.TryAdd("ext1", "extvalue1");

            // Act
            variables.Add(externalVars);

            // Assert
            variables.AllVariables.Should().NotBeNull();
            variables.AllVariables.Should().HaveCount(2);
            variables.AllVariables.Should().ContainKey("var1");
            variables.AllVariables.Should().ContainKey("ext1");
        }

        [Fact]
        public void Add_WhenCalledMultipleTimes_ShouldOnlyAddOnce()
        {
            // Arrange
            var variables = new Variables
            {
                VariableList = new Collection<Variable>
                {
                    new Variable { Name = "var1", Value = "value1" }
                }
            };

            // Act
            variables.Add(null);
            variables.Add(null);

            // Assert
            variables.AllVariables.Should().HaveCount(1);
        }

        [Fact]
        public void Add_WithNullNameOrValue_ShouldSkipVariable()
        {
            // Arrange
            var variables = new Variables
            {
                VariableList = new Collection<Variable>
                {
                    new Variable { Name = null, Value = "value1" },
                    new Variable { Name = "var2", Value = null },
                    new Variable { Name = "var3", Value = "value3" }
                }
            };

            // Act
            variables.Add(null);

            // Assert
            variables.AllVariables.Should().NotBeNull();
            variables.AllVariables.Should().HaveCount(1);
            variables.AllVariables.Should().ContainKey("var3");
        }

        [Fact]
        public void Add_WithCaseInsensitiveNames_ShouldTreatAsSame()
        {
            // Arrange
            var variables = new Variables
            {
                VariableList = new Collection<Variable>
                {
                    new Variable { Name = "VAR1", Value = "value1" }
                }
            };
            var externalVars = new ConcurrentDictionary<string, string>();
            externalVars.TryAdd("var1", "value2");

            // Act
            variables.Add(externalVars);

            // Assert
            variables.AllVariables.Should().NotBeNull();
            variables.AllVariables.Should().HaveCount(1);
            variables.AllVariables!["VAR1"].Should().Be("value1"); // First one wins
        }

        [Fact]
        public void Add_WithEmptyVariableList_ShouldNotThrow()
        {
            // Arrange
            var variables = new Variables
            {
                VariableList = new Collection<Variable>()
            };

            // Act
            System.Action act = () => variables.Add(null);

            // Assert
            act.Should().NotThrow();
            variables.AllVariables.Should().NotBeNull();
            variables.AllVariables.Should().BeEmpty();
        }
    }
}
