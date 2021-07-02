using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TE.FileWatcher.Logging;

namespace TE.FileWatcher.Notifications
{
    /// <summary>
    /// The notification triggers.
    /// </summary>
    [Flags]
    public enum NotificationTriggers
    {
        /// <summary>
        /// No triggers are specified.
        /// </summary>
        None = 0,
        /// <summary>
        /// Change notification.
        /// </summary>
        Change = 1,
        /// <summary>
        /// Create notification.
        /// </summary>
        Create = 2,
        /// <summary>
        /// Delete notification.
        /// </summary>
        Delete = 4,
        /// <summary>
        /// Rename notification.
        /// </summary>
        Rename = 8
    }

    /// <summary>
    /// The notifications root node in the XML file.
    /// </summary>
    [XmlRoot("notifications")]
    public class Notifications
    {
        // The timer
        private Timer _timer;
        
        /// <summary>
        /// Gets or sets the notifications list.
        /// </summary>
        [XmlElement("notification")]
        public List<Notification> NotificationList { get; set; } = new List<Notification>();

        /// <summary>
        /// Initializes an instance of the <see cref="Notifications"/> class.
        /// </summary>
        public Notifications()
        {            
            _timer = new Timer(60000);
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
                    using (HttpResponseMessage response = await notification.SendAsync())
                    {
                        if (response == null)
                        {
                            continue;
                        }

                        Logger.WriteLine($"Response: {response.StatusCode}.");
                        using (HttpContent httpContent = response.Content)
                        {
                            string resultContent = await httpContent.ReadAsStringAsync();
                            Logger.WriteLine($"Content: {resultContent}");
                        }
                    }
                }
                catch (NullReferenceException ex)
                {
                    Logger.WriteLine(ex.Message, LogLevel.ERROR);
                }
            }
        }
    }
}
