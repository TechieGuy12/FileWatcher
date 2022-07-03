using System.Collections.ObjectModel;
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
    public class Notifications : IDisposable
    {
        // The default wait time
        private const int DEFAULT_WAIT_TIME = 60000;

        // The minimum wait time
        private const int MIN_WAIT_TIME = 30000;

        // The timer
        private readonly System.Timers.Timer _timer;

        // Flag indicating the class is disposed
        private bool _disposed;

        /// <summary>
        /// Gets or sets the wait time between notification requests.
        /// </summary>
        [XmlElement("waittime")]
        public int WaitTime { get; set; } = DEFAULT_WAIT_TIME;

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
            _timer = new System.Timers.Timer(WaitTime);
            _timer.Elapsed += OnElapsed;
            _timer.Start();
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
                if (_timer != null)
                {
                    _timer.Dispose();
                }
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
        /// The information associated witht he elapsed time.
        /// </param>
        private async void OnElapsed(object? source, ElapsedEventArgs e)
        {
            // If there are no notifications, then stop the timer
            if (NotificationList == null || NotificationList.Count <= 0)
            {
                _timer.Stop();
                return;
            }

            // Ensure the wait time is not less than the minimum wait time
            if (WaitTime < MIN_WAIT_TIME)
            {
                Logger.WriteLine($"The wait time {WaitTime} is below the minimum of {MIN_WAIT_TIME}. Setting wait time to {MIN_WAIT_TIME}.");
                WaitTime = MIN_WAIT_TIME;
            }

            foreach (Notification notification in NotificationList)
            {
                // If the notification doesn't have a message to send, then
                // continue to the next notification
                if (!notification.HasMessage)
                {
                    continue;
                }

                try
                {
                    Logger.WriteLine($"Sending the request to {notification.Url}.");
                    Response? response =
                        await notification.SendAsync().ConfigureAwait(false);
                    
                    if (response == null)
                    {
                        continue;
                    }

                    Logger.WriteLine($"Response: {response.StatusCode}. Content: {response.Content}");
                    
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
                catch (NullReferenceException ex)
                {
                    Logger.WriteLine(ex.Message, LogLevel.ERROR);
                    Logger.WriteLine(
                        $"StackTrace:{Environment.NewLine}{ex.StackTrace}",
                        LogLevel.ERROR);
                }
            }
        }

        /// <summary>
        /// Sends the notification request.
        /// </summary>
        /// <param name="trigger">
        /// The trigger associated with the request.
        /// </param>
        /// <param name="message">
        /// The message to include in the request.
        /// </param>
        public void Send(TriggerType trigger, string message)
        {
            if (NotificationList == null || NotificationList.Count <= 0)
            {
                return;
            }

            foreach (Notification notification in NotificationList)
            {
                notification.QueueRequest(message, trigger);
            }
        }
    }
}
