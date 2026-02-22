using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Timers;
using System.Xml.Serialization;
using TE.FileWatcher.Log;
using TE.FileWatcher.Net;

namespace TE.FileWatcher.Configuration
{
    /// <summary>
    /// The notifications root node in the XML file.
    /// </summary>
    [XmlRoot("notifications")]
    public class Notifications : HasVariablesBase, IDisposable
    {
        // The default wait time (1 second)
        private const int DEFAULT_WAIT_TIME_MS = 1000;

        // The minimum wait time (1 second)
        private const int MIN_WAIT_TIME_MS = 1000;

        // The timer
        private readonly System.Timers.Timer _timer;

        // Flag indicating the class is disposed
        private bool _disposed;

        private int currentWaitTime;

        /// <summary>
        /// Gets or sets the wait time between notification requests.
        /// </summary>
        [XmlElement("waittime")]
        public int? WaitTime { get; set; }

        /// <summary>
        /// Gets or sets the notifications list.
        /// </summary>
        [XmlElement("notification")]
        public Collection<Notification>? NotificationList { get; set; }

        /// <summary>
        /// Initializes an instance of the <see cref="Notifications"/> class.
        /// </summary>
        public Notifications()
        {
            // Initialize with default wait time - actual wait time will be set when timer starts
            currentWaitTime = DEFAULT_WAIT_TIME_MS;

            _timer = new System.Timers.Timer(DEFAULT_WAIT_TIME_MS);
            _timer.Elapsed += OnElapsed;            
        }

        /// <summary>
        /// Add the variables list to the dependent objects.
        /// </summary>
        private void AddVariables()
        {
            if (NotificationList == null)
            {
                return;
            }

            foreach (Notification notification in NotificationList)
            {
                notification.Variables ??= new Variables();
                notification.Variables.Add(Variables?.AllVariables);
            }
        }

        /// <summary>
        /// Releases all resources used by the class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Release all resources used by the class.
        /// </summary>
        /// <param name="disposing">
        /// Indicates the whether the class is disposing.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _timer?.Dispose();                
            }

            _disposed = true;
        }

        /// <summary>
        /// Called when the timers elapsed time has been reached.
        /// </summary>
        /// <param name="source">
        /// The timer object.
        /// </param>
        /// <param name="e">
        /// The information associated with the elapsed time.
        /// </param>
        private void OnElapsed(object? source, ElapsedEventArgs e)
        {
            // Fire and forget pattern - don't await, but handle exceptions
            _ = ProcessNotificationsAsync();
        }

        /// <summary>
        /// Processes all queued notifications asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ProcessNotificationsAsync()
        {
            try
            {
                if (Logger.LogLevel <= LogLevel.DEBUG)
                {
                    Logger.WriteLine("Notifications.ProcessNotificationsAsync: Timer elapsed, processing notifications.", LogLevel.DEBUG);
                }

                // If there are no notifications, then stop the timer
                if (NotificationList == null || NotificationList.Count <= 0)
                {
                    _timer.Stop();
                    if (Logger.LogLevel <= LogLevel.DEBUG)
                    {
                        Logger.WriteLine("Notifications.ProcessNotificationsAsync: No notifications configured, stopping timer.", LogLevel.DEBUG);
                    }
                    return;
                }

                int processedCount = 0;
                int skippedCount = 0;

                foreach (Notification notification in NotificationList)
                {
                    // If the notification doesn't have a message to send, then
                    // continue to the next notification
                    if (!notification.HasMessage)
                    {
                        skippedCount++;
                        continue;
                    }

                    try
                    {
                        if (Logger.LogLevel <= LogLevel.DEBUG)
                        {
                            Logger.WriteLine($"Notifications.ProcessNotificationsAsync: Sending notification {processedCount + 1}.", LogLevel.DEBUG);
                        }

                        Response? response =
                            await notification.SendAsync().ConfigureAwait(false);
                        
                        if (response == null)
                        {
                            continue;
                        }

                        Logger.WriteLine($"Response: {response.StatusCode}. URL: {response.Url}. Content: {response.Content}");
                        processedCount++;
                        
                    }
                    catch (AggregateException aex)
                    {
                        foreach (Exception ex in aex.Flatten().InnerExceptions)
                        {
                            Logger.WriteLine(ex.Message, LogLevel.ERROR);
                            Logger.WriteLine(
                                $"StackTrace:{Environment.NewLine}{ex.StackTrace}",
                                LogLevel.ERROR);
                        }
                    }
                    catch (Exception ex)
                        when (ex is NullReferenceException || ex is InvalidOperationException || ex is UriFormatException)
                    {
                        Logger.WriteLine(ex.Message, LogLevel.ERROR);
                        Logger.WriteLine(
                            $"StackTrace:{Environment.NewLine}{ex.StackTrace}",
                            LogLevel.ERROR);
                    }
                }

                // Stop the timer when there are no more messages pending across all notifications
                bool hasPendingMessages = NotificationList.Any(n => n.HasMessage);
                if (!hasPendingMessages)
                {
                    _timer.Stop();
                }

                if (Logger.LogLevel <= LogLevel.DEBUG)
                {
                    Logger.WriteLine($"Notifications.ProcessNotificationsAsync: Processed {processedCount} notification(s), skipped {skippedCount}. Timer stopped: {!_timer.Enabled}.", LogLevel.DEBUG);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Unhandled exception in notification processing: {ex.Message}", LogLevel.FATAL);
                Logger.WriteLine($"StackTrace:{Environment.NewLine}{ex.StackTrace}", LogLevel.FATAL);
            }
        }

        /// <summary>
        /// Sends the notification request.
        /// </summary>
        /// <param name="trigger">
        /// The trigger associated with the request.
        /// </param>
        /// <param name="change">
        /// Information about the change.
        /// </param>
        public void Send(TriggerType trigger, ChangeInfo change)
        {
            if (change == null)
            {
                if (Logger.LogLevel <= LogLevel.DEBUG)
                {
                    Logger.WriteLine("Notifications.Send: Change is null, skipping.", LogLevel.DEBUG);
                }
                return;
            }

            if (NotificationList == null || NotificationList.Count <= 0)
            {
                if (Logger.LogLevel <= LogLevel.DEBUG)
                {
                    Logger.WriteLine($"[{change.CorrelationId}] Notifications.Send: No notifications configured, skipping.", LogLevel.DEBUG);
                }
                return;
            }

            if (Logger.LogLevel <= LogLevel.DEBUG)
            {
                Logger.WriteLine($"[{change.CorrelationId}] Notifications.Send: Processing {NotificationList.Count} notification(s) for trigger {trigger}.", LogLevel.DEBUG);
            }

            AddVariables();

            int queuedCount = 0;
            foreach (Notification notification in NotificationList)
            {
                bool hadMessageBefore = notification.HasMessage;
                notification.QueueRequest(trigger, change);
                bool hadMessageAfter = notification.HasMessage;
                
                if (!hadMessageBefore && hadMessageAfter)
                {
                    queuedCount++;
                }
            }

            if (Logger.LogLevel <= LogLevel.DEBUG)
            {
                Logger.WriteLine($"[{change.CorrelationId}] Notifications.Send: Queued {queuedCount} notification(s). Timer enabled: {_timer.Enabled}.", LogLevel.DEBUG);
            }

            if (!_timer.Enabled)
            {
                currentWaitTime = WaitTime ?? DEFAULT_WAIT_TIME_MS;              
                if (currentWaitTime < MIN_WAIT_TIME_MS)
                {
                    currentWaitTime = MIN_WAIT_TIME_MS;
                }

                _timer.Interval = currentWaitTime;
                _timer.Start();
                
                if (Logger.LogLevel <= LogLevel.DEBUG)
                {
                    Logger.WriteLine($"[{change.CorrelationId}] Notifications.Send: Started timer with interval {currentWaitTime}ms.", LogLevel.DEBUG);
                }
            }
        }
    }
}
