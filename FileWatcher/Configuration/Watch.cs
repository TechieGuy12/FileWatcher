using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TE.FileWatcher.Configuration
{
    /// <summary>
    /// The watch element in the XML file.
    /// </summary>
    public class Watch
    {
        /// <summary>
        /// Gets or sets the path of the watch.
        /// </summary>
        [XmlElement("path")]
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the exclusions
        /// </summary>
        [XmlElement("exclusions")]
        public Exclusions.Exclusions Exclusions { get; set; } = new Exclusions.Exclusions();

        /// <summary>
        /// Gets or sets the notifications for the watch.
        /// </summary>
        [XmlElement("notifications")]
        public Notifications.Notifications Notifications { get; set; } = new Notifications.Notifications();

        /// <summary>
        /// Gets or sets the actions for the watch.
        /// </summary>
        [XmlElement("actions")]
        public Actions.Actions Actions { get; set; } = new Actions.Actions();

        /// <summary>
        /// Processes the file or folder change.
        /// </summary>
        /// <param name="trigger">
        /// The type of change.
        /// </param>
        /// <param name="name">
        /// The name of the file or folder.
        /// </param>
        /// <param name="fullPath">
        /// The full path of the file or folder.
        /// </param>
        public void ProcessChange(
            Notifications.NotificationTriggers trigger,
            string name, 
            string fullPath)
        {
            if (Exclusions.Exclude(Path, name, fullPath))
            {
                return;
            }

            string messageType = null;
            switch (trigger)
            {
                case Configuration.Notifications.NotificationTriggers.Create:
                    messageType = "Created";
                    break;
                case Configuration.Notifications.NotificationTriggers.Change:
                    messageType = "Changed";
                    break;
                case Configuration.Notifications.NotificationTriggers.Delete:
                    messageType = "Deleted";
                    break;
                case Configuration.Notifications.NotificationTriggers.Rename:
                    messageType = "Renamed";
                    break;
            }

            if (!string.IsNullOrWhiteSpace(messageType))
            {
                Notifications.Send(trigger, $"{messageType}: {fullPath}");
            }
        }
    }
}
