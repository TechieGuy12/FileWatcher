using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TE.FileWatcher.Configuration.Notifications
{
    public class Triggers
    {
        [XmlElement("trigger")]
        public List<string> TriggerList { get; set; } = new List<string>();

        [XmlIgnore]
        public NotificationTriggers NotificationTriggers
        {
            get
            {
                NotificationTriggers triggers = NotificationTriggers.None;
                foreach(string trigger in TriggerList)
                {
                    try
                    {
                        triggers |= (NotificationTriggers)Enum.Parse(typeof(NotificationTriggers), trigger);

                    }
                    // Ignore any exceptions if a trigger could be parsed
                    catch { }
                }

                return triggers;
            }
        }
    }
}
