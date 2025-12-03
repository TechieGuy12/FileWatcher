using FluentAssertions;
using TE.FileWatcher.Configuration;
using Xunit;

namespace FileWatcher.Tests.Configuration
{
    public class TriggersTests
    {
        [Fact]
        public void Current_EmptyTriggerList_ShouldReturnNone()
        {
            // Arrange
            var triggers = new Triggers
            {
                TriggerList = new System.Collections.ObjectModel.Collection<TriggerType>()
            };

            // Act
            var result = triggers.Current;

            // Assert
            result.Should().Be(TriggerType.None);
        }

        [Fact]
        public void Current_NullTriggerList_ShouldReturnNone()
        {
            // Arrange
            var triggers = new Triggers
            {
                TriggerList = null
            };

            // Act
            var result = triggers.Current;

            // Assert
            result.Should().Be(TriggerType.None);
        }

        [Fact]
        public void Current_SingleTrigger_ShouldReturnThatTrigger()
        {
            // Arrange
            var triggers = new Triggers
            {
                TriggerList = new System.Collections.ObjectModel.Collection<TriggerType>
                {
                    TriggerType.Create
                }
            };

            // Act
            var result = triggers.Current;

            // Assert
            result.Should().Be(TriggerType.Create);
        }

        [Fact]
        public void Current_MultipleTriggers_ShouldReturnCombinedFlags()
        {
            // Arrange
            var triggers = new Triggers
            {
                TriggerList = new System.Collections.ObjectModel.Collection<TriggerType>
                {
                    TriggerType.Create,
                    TriggerType.Change,
                    TriggerType.Delete
                }
            };

            // Act
            var result = triggers.Current;

            // Assert
            result.Should().HaveFlag(TriggerType.Create);
            result.Should().HaveFlag(TriggerType.Change);
            result.Should().HaveFlag(TriggerType.Delete);
            result.Should().NotHaveFlag(TriggerType.Rename);
        }

        [Fact]
        public void Current_AllTriggers_ShouldReturnAllFlags()
        {
            // Arrange
            var triggers = new Triggers
            {
                TriggerList = new System.Collections.ObjectModel.Collection<TriggerType>
                {
                    TriggerType.Create,
                    TriggerType.Change,
                    TriggerType.Delete,
                    TriggerType.Rename
                }
            };

            // Act
            var result = triggers.Current;

            // Assert
            result.Should().HaveFlag(TriggerType.Create);
            result.Should().HaveFlag(TriggerType.Change);
            result.Should().HaveFlag(TriggerType.Delete);
            result.Should().HaveFlag(TriggerType.Rename);
        }

        [Fact]
        public void Current_CalledMultipleTimes_ShouldReturnSameResult()
        {
            // Arrange
            var triggers = new Triggers
            {
                TriggerList = new System.Collections.ObjectModel.Collection<TriggerType>
                {
                    TriggerType.Create,
                    TriggerType.Change
                }
            };

            // Act
            var result1 = triggers.Current;
            var result2 = triggers.Current;
            var result3 = triggers.Current;

            // Assert
            result1.Should().Be(result2);
            result2.Should().Be(result3);
        }

        [Fact]
        public void Current_ThreadSafety_ShouldHandleConcurrentAccess()
        {
            // Arrange
            var triggers = new Triggers
            {
                TriggerList = new System.Collections.ObjectModel.Collection<TriggerType>
                {
                    TriggerType.Create,
                    TriggerType.Change,
                    TriggerType.Delete,
                    TriggerType.Rename
                }
            };

            var expectedResult = TriggerType.Create | TriggerType.Change | TriggerType.Delete | TriggerType.Rename;
            var results = new System.Collections.Concurrent.ConcurrentBag<TriggerType>();

            // Act - Access Current property from multiple threads simultaneously
            Parallel.For(0, 100, _ =>
            {
                results.Add(triggers.Current);
            });

            // Assert - All results should be the same
            results.Should().HaveCount(100);
            results.Should().AllSatisfy(r => r.Should().Be(expectedResult));
        }

        [Fact]
        public void Current_DuplicateTriggers_ShouldHandleGracefully()
        {
            // Arrange
            var triggers = new Triggers
            {
                TriggerList = new System.Collections.ObjectModel.Collection<TriggerType>
                {
                    TriggerType.Create,
                    TriggerType.Create, // Duplicate
                    TriggerType.Change
                }
            };

            // Act
            var result = triggers.Current;

            // Assert
            result.Should().HaveFlag(TriggerType.Create);
            result.Should().HaveFlag(TriggerType.Change);
            result.Should().NotHaveFlag(TriggerType.Delete);
        }
    }
}
