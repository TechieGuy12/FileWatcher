using FluentAssertions;
using TE.FileWatcher.Configuration;
using Xunit;
using ConfigAction = TE.FileWatcher.Configuration.Action;

namespace FileWatcher.Tests.Configuration
{
    public class NotificationsTests : IDisposable
    {
        private Notifications? _notifications;

        public void Dispose()
        {
            _notifications?.Dispose();
        }

        [Fact]
        public void Constructor_ShouldInitializeTimer()
        {
            // Act
            _notifications = new Notifications();

            // Assert
            _notifications.Should().NotBeNull();
        }

        [Fact]
        public void Send_NullChange_ShouldNotThrow()
        {
            // Arrange
            _notifications = new Notifications();

            // Act
            System.Action act = () => _notifications.Send(TriggerType.Create, null!);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Send_EmptyNotificationList_ShouldNotThrow()
        {
            // Arrange
            _notifications = new Notifications
            {
                NotificationList = new System.Collections.ObjectModel.Collection<Notification>()
            };

            var change = new ChangeInfo(
                TriggerType.Create,
                @"C:\test",
                "file.txt",
                @"C:\test\file.txt",
                null,
                null
            );

            // Act
            System.Action act = () => _notifications.Send(TriggerType.Create, change);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Dispose_ShouldNotThrowWhenCalledMultipleTimes()
        {
            // Arrange
            _notifications = new Notifications();

            // Act
            System.Action act = () =>
            {
                _notifications.Dispose();
                _notifications.Dispose();
                _notifications.Dispose();
            };

            // Assert
            act.Should().NotThrow();
        }
    }

    public class ActionsTests
    {
        [Fact]
        public void Run_DeleteTrigger_ShouldReturnEarly()
        {
            // Arrange
            var actions = new Actions
            {
                ActionList = new System.Collections.ObjectModel.Collection<ConfigAction>()
            };

            var change = new ChangeInfo(
                TriggerType.Delete,
                @"C:\test",
                "file.txt",
                @"C:\test\file.txt",
                null,
                null
            );

            // Act
            System.Action act = () => actions.Run(TriggerType.Delete, change);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Run_NullActionList_ShouldNotThrow()
        {
            // Arrange
            var actions = new Actions
            {
                ActionList = null
            };

            var change = new ChangeInfo(
                TriggerType.Create,
                @"C:\test",
                "file.txt",
                @"C:\test\file.txt",
                null,
                null
            );

            // Act
            System.Action act = () => actions.Run(TriggerType.Create, change);

            // Assert
            act.Should().NotThrow();
        }
    }

    public class CommandsTests
    {
        [Fact]
        public void Run_NullCommandList_ShouldNotThrow()
        {
            // Arrange
            var commands = new Commands
            {
                CommandList = null
            };

            var change = new ChangeInfo(
                TriggerType.Create,
                @"C:\test",
                "file.txt",
                @"C:\test\file.txt",
                null,
                null
            );

            // Act
            System.Action act = () => commands.Run(TriggerType.Create, change);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Run_EmptyCommandList_ShouldNotThrow()
        {
            // Arrange
            var commands = new Commands
            {
                CommandList = new System.Collections.ObjectModel.Collection<Command>()
            };

            var change = new ChangeInfo(
                TriggerType.Create,
                @"C:\test",
                "file.txt",
                @"C:\test\file.txt",
                null,
                null
            );

            // Act
            System.Action act = () => commands.Run(TriggerType.Create, change);

            // Assert
            act.Should().NotThrow();
        }
    }
}
