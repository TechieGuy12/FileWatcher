using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TE.FileWatcher.Notifications
{
    [Flags]
    public enum NotificationTriggers
    {
        None = 0,        
        Change = 1,
        Create = 2,
        Delete = 4,
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
                try
                {
                    using (HttpResponseMessage response = await notification.SendAsync())
                    {
                        Console.WriteLine($"Response: {response.StatusCode}.");
                        using (HttpContent httpContent = response.Content)
                        {
                            string resultContent = await httpContent.ReadAsStringAsync();
                            Console.WriteLine($"Content: {resultContent}");
                        }
                    }
                }
                catch (NullReferenceException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }


    }
}
