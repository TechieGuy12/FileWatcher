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

        private void OnElapsed(object source, ElapsedEventArgs e)
        {
            foreach (Notification notification in NotificationList)
            {
                notification.Send();
            }
        }


    }
}
