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
        /// Gets the string value for the message type.
        /// </summary>
        /// <param name="trigger">
        /// The notification trigger.
        /// </param>
        /// <returns>
        /// The string value for the message type, otherwise <c>null</c>.
        /// </returns>
        private string GetMessageTypeString(TriggerType trigger)
        {
            string messageType = null;
            switch (trigger)
            {
                case TriggerType.Create:
                    messageType = "Created";
                    break;
                case TriggerType.Change:
                    messageType = "Changed";
                    break;
                case TriggerType.Delete:
                    messageType = "Deleted";
                    break;
                case TriggerType.Rename:
                    messageType = "Renamed";
                    break;
            }

            return messageType;
        }

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
            TriggerType trigger,
            string name, 
            string fullPath)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(fullPath))
            {
                return;
            }

            // If the file or folder is in the exclude list, then don't take
            // any further actions
            if (Exclusions.Exclude(Path, name, fullPath))
            {
                return;
            }

            // Send the notifications
            string messageType = GetMessageTypeString(trigger);
            if (!string.IsNullOrWhiteSpace(messageType))
            {
                Notifications.Send(trigger, $"{messageType}: {fullPath}");
            }

            // Only run the actions if a file wasn't deleted, as the file no
            // longer exists so no action can be taken on the file
            if (trigger != TriggerType.Delete)
            {
                Actions.Run(trigger, Path, fullPath);
            }
        }
    }
}
