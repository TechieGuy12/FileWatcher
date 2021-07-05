using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TE.FileWatcher.Configuration.Notifications
{
    /// <summary>
    /// The triggers that will indicate a notification is to be sent.
    /// </summary>
    public class Triggers
    {
        // The flags for the triggers
        NotificationTriggers _triggers = NotificationTriggers.None;

        /// <summary>
        /// Gets or sets a list of notification triggers.
        /// </summary>
        [XmlElement("trigger")]
        public List<NotificationTriggers> TriggerList { get; set; } = new List<NotificationTriggers>();

        /// <summary>
        /// Gets the triggers for the notification using the list from the
        /// <see cref="TriggerList"/> property.
        /// </summary>
        [XmlIgnore]
        public NotificationTriggers NotificationTriggers
        {
            get
            {
                if (_triggers != NotificationTriggers.None)
                {
                    return _triggers;
                }

                foreach(NotificationTriggers trigger in TriggerList)
                {
                    _triggers |= trigger;
                }

                return _triggers;
            }
        }
    }
}
