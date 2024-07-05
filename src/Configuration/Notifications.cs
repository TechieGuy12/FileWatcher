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
        // The default wait time
        private const int DEFAULT_WAIT_TIME = 30000;

        // The minimum wait time
        private const int MIN_WAIT_TIME = 30000;

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
            currentWaitTime = WaitTime ?? DEFAULT_WAIT_TIME;

            _timer = new System.Timers.Timer(currentWaitTime);
            _timer.Elapsed += OnElapsed;            
        }

        /// <summary>
        /// Add the variables list to the dependent objects.
        /// </summary>
        private void AddVariables()
        {
            if (NotificationList != null)
            {
                Parallel.ForEach(NotificationList, (notification) =>
                {
                    notification.Variables ??= new Variables();
                    notification.Variables.Add(Variables?.AllVariables);
                });
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
                    Response? response =
                        await notification.SendAsync().ConfigureAwait(false);
                    
                    if (response == null)
                    {
                        continue;
                    }

                    Logger.WriteLine($"Response: {response.StatusCode}. URL: {response.Url}. Content: {response.Content}");
                    
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

            if (NotificationList.Count <= 0)
            {
                _timer.Stop();
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
                return;
            }

            if (NotificationList == null || NotificationList.Count <= 0)
            {
                return;
            }

            AddVariables();

            foreach (Notification notification in NotificationList)
            {
                notification.QueueRequest(trigger, change);
            }

            if (!_timer.Enabled)
            {
                currentWaitTime = WaitTime ?? DEFAULT_WAIT_TIME;              
                if (currentWaitTime < MIN_WAIT_TIME)
                {
                    currentWaitTime = MIN_WAIT_TIME;
                }

                _timer.Interval = currentWaitTime;
                _timer.Start();
            }
        }
    }
}
