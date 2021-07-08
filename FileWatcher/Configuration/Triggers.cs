using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TE.FileWatcher.Configuration
{
    /// <summary>
    /// The notification triggers.
    /// </summary>
    [Flags]
    [Serializable]
    public enum TriggerType
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
    /// The triggers that will indicate a notification is to be sent.
    /// </summary>
    public class Triggers
    {
        // The flags for the triggers
        TriggerType _triggers = TriggerType.None;

        /// <summary>
        /// Gets or sets a list of notification triggers.
        /// </summary>
        [XmlElement("trigger")]
        public List<TriggerType> TriggerList { get; set; } = new List<TriggerType>();

        /// <summary>
        /// Gets the current combined triggers using the list from the
        /// <see cref="TriggerList"/> property.
        /// </summary>
        [XmlIgnore]
        public TriggerType Current
        {
            get
            {
                if (_triggers != TriggerType.None)
                {
                    return _triggers;
                }

                foreach(TriggerType trigger in TriggerList)
                {
                    _triggers |= trigger;
                }

                return _triggers;
            }
        }
    }
}
