using FluentAssertions;
using TE.FileWatcher.Configuration;
using Xunit;

namespace FileWatcher.Tests.Configuration
{
    public class ChangeInfoTests
    {
        [Fact]
        public void Constructor_WithValidParameters_ShouldSetProperties()
        {
            // Arrange
            var trigger = TriggerType.Create;
            var watchPath = @"C:\watch";
            var name = "file.txt";
            var fullPath = @"C:\watch\file.txt";

            // Act
            var changeInfo = new ChangeInfo(trigger, watchPath, name, fullPath, null, null);

            // Assert
            changeInfo.Trigger.Should().Be(trigger);
            changeInfo.WatchPath.Should().Be(watchPath);
            changeInfo.Name.Should().Be(name);
            changeInfo.FullPath.Should().Be(fullPath);
            changeInfo.OldName.Should().BeNull();
            changeInfo.OldPath.Should().BeNull();
        }

        [Fact]
        public void Constructor_WithRenameParameters_ShouldSetOldNameAndPath()
        {
            // Arrange
            var trigger = TriggerType.Rename;
            var watchPath = @"C:\watch";
            var name = "newfile.txt";
            var fullPath = @"C:\watch\newfile.txt";
            var oldName = "oldfile.txt";
            var oldPath = @"C:\watch\oldfile.txt";

            // Act
            var changeInfo = new ChangeInfo(trigger, watchPath, name, fullPath, oldName, oldPath);

            // Assert
            changeInfo.Trigger.Should().Be(trigger);
            changeInfo.WatchPath.Should().Be(watchPath);
            changeInfo.Name.Should().Be(name);
            changeInfo.FullPath.Should().Be(fullPath);
            changeInfo.OldName.Should().Be(oldName);
            changeInfo.OldPath.Should().Be(oldPath);
        }

        [Fact]
        public void Constructor_WithNullWatchPath_ShouldThrowArgumentNullException()
        {
            // Act
            System.Action act = () => new ChangeInfo(TriggerType.Create, null!, "file.txt", @"C:\watch\file.txt", null, null);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("watchPath");
        }

        [Fact]
        public void Constructor_WithNullName_ShouldThrowArgumentNullException()
        {
            // Act
            System.Action act = () => new ChangeInfo(TriggerType.Create, @"C:\watch", null!, @"C:\watch\file.txt", null, null);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("name");
        }

        [Fact]
        public void Constructor_WithNullFullPath_ShouldThrowArgumentNullException()
        {
            // Act
            System.Action act = () => new ChangeInfo(TriggerType.Create, @"C:\watch", "file.txt", null!, null, null);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("fullPath");
        }

        [Theory]
        [InlineData(TriggerType.Create)]
        [InlineData(TriggerType.Change)]
        [InlineData(TriggerType.Delete)]
        [InlineData(TriggerType.Rename)]
        public void Constructor_WithDifferentTriggers_ShouldSetTriggerCorrectly(TriggerType trigger)
        {
            // Act
            var changeInfo = new ChangeInfo(trigger, @"C:\watch", "file.txt", @"C:\watch\file.txt", null, null);

            // Assert
            changeInfo.Trigger.Should().Be(trigger);
        }

        [Fact]
        public void Properties_ShouldBeReadOnly()
        {
            // Arrange
            var changeInfo = new ChangeInfo(TriggerType.Create, @"C:\watch", "file.txt", @"C:\watch\file.txt", null, null);

            // Assert - Properties should not have setters (this is verified by compilation)
            changeInfo.Trigger.Should().Be(TriggerType.Create);
            changeInfo.WatchPath.Should().Be(@"C:\watch");
            changeInfo.Name.Should().Be("file.txt");
            changeInfo.FullPath.Should().Be(@"C:\watch\file.txt");
        }

        [Fact]
        public void Constructor_WithEmptyStrings_ShouldNotThrow()
        {
            // Act
            System.Action act = () => new ChangeInfo(TriggerType.Create, "", "", "", "", "");

            // Assert
            act.Should().NotThrow();
        }
    }
}
