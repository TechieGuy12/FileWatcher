using FluentAssertions;
using System.Collections.ObjectModel;
using TE.FileWatcher.Configuration;
using Xunit;

namespace FileWatcher.Tests.Configuration
{
    // Concrete implementation for testing abstract ItemBase
    public class TestItem : ItemBase
    {
    }

    public class ItemBaseTests
    {
        [Fact]
        public void ItemBase_ShouldInitializeWithNullChange()
        {
            // Act
            var item = new TestItem();

            // Assert
            item.Change.Should().BeNull();
        }

        [Fact]
        public void ItemBase_ShouldInitializeWithDefaultTriggers()
        {
            // Act
            var item = new TestItem();

            // Assert
            item.Triggers.Should().NotBeNull();
        }

        [Fact]
        public void ItemBase_ShouldSetChangeCorrectly()
        {
            // Arrange
            var item = new TestItem();
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);

            // Act
            item.Change = change;

            // Assert
            item.Change.Should().BeSameAs(change);
        }

        [Fact]
        public void ItemBase_ShouldSetTriggersCorrectly()
        {
            // Arrange
            var item = new TestItem();
            var triggers = new Triggers
            {
                TriggerList = new Collection<TriggerType> { TriggerType.Create, TriggerType.Change }
            };

            // Act
            item.Triggers = triggers;

            // Assert
            item.Triggers.Should().BeSameAs(triggers);
            item.Triggers.TriggerList.Should().HaveCount(2);
        }

        [Fact]
        public void ItemBase_WithMultipleChanges_ShouldKeepLatest()
        {
            // Arrange
            var item = new TestItem();
            var change1 = new ChangeInfo(TriggerType.Create, "C:\\watch", "file1.txt", "C:\\watch\\file1.txt", null, null);
            var change2 = new ChangeInfo(TriggerType.Change, "C:\\watch", "file2.txt", "C:\\watch\\file2.txt", null, null);

            // Act
            item.Change = change1;
            item.Change = change2;

            // Assert
            item.Change.Should().BeSameAs(change2);
        }

        [Fact]
        public void ItemBase_CanSetChangeToNull()
        {
            // Arrange
            var item = new TestItem();
            var change = new ChangeInfo(TriggerType.Create, "C:\\watch", "file.txt", "C:\\watch\\file.txt", null, null);
            item.Change = change;

            // Act
            item.Change = null;

            // Assert
            item.Change.Should().BeNull();
        }
    }
}
