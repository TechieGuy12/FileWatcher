using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TE.FileWatcher.Logging;

namespace TE.FileWatcher.Configuration.Notifications
{
    /// <summary>
    /// The notifications root node in the XML file.
    /// </summary>
    [XmlRoot("notifications")]
    public class Notifications
    {
        // The default wait time
        private const int DEFAULT_WAIT_TIME = 60000;

        // The minimum wait time
        private const int MIN_WAIT_TIME = 30000;

        // The timer
        private Timer _timer;

        /// <summary>
        /// Gets or sets the wait time between notification requests.
        /// </summary>
        [XmlElement("waittime")]
        public int WaitTime { get; set; } = DEFAULT_WAIT_TIME;

        /// <summary>
        /// Gets or sets the notifications list.
        /// </summary>
        [XmlElement("notification")]
        public List<Notification> NotificationList { get; set; }

        /// <summary>
        /// Initializes an instance of the <see cref="Notifications"/> class.
        /// </summary>
        public Notifications()
        {            
            _timer = new Timer(WaitTime);
            _timer.Elapsed += OnElapsed;
            _timer.Start();
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
        private async void OnElapsed(object source, ElapsedEventArgs e)
        {
            // If there are no notifications, then stop the timer
            if (NotificationList?.Count <= 0)
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
                    using (HttpResponseMessage response = await notification.SendAsync())
                    {
                        if (response == null)
                        {
                            continue;
                        }

                        using (HttpContent httpContent = response.Content)
                        {
                            string resultContent = await httpContent.ReadAsStringAsync();
                            Logger.WriteLine($"Response: {response.StatusCode}. Content: {resultContent}");
                        }
                    }
                }
                catch (AggregateException aex)
                {
                    foreach (Exception ex in aex.Flatten().InnerExceptions)
                    {
                        Logger.WriteLine(ex.Message, LogLevel.ERROR);
                        Logger.WriteLine($"StackTrace:{Environment.NewLine}{ex.StackTrace}");
                    }
                }
                catch (NullReferenceException ex)
                {
                    Logger.WriteLine(ex.Message, LogLevel.ERROR);
                    Logger.WriteLine($"StackTrace:{Environment.NewLine}{ex.StackTrace}");
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
            if (NotificationList?.Count <= 0)
            {
                return;
            }

            foreach (Notification notification in NotificationList)
            {
                if (notification.Triggers?.TriggerList.Count > 0)
                {
                    if (notification.Triggers.Current.HasFlag(trigger))
                    {
                        notification.QueueRequest(message);
                    }
                }
            }
        }
    }
}
