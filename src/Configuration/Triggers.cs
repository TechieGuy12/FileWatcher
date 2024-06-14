﻿using System.Collections.ObjectModel;
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
        Rename = 8,
        /// <summary>
        /// A step is being executed.
        /// </summary>
        Step = 16
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
        public Collection<TriggerType>? TriggerList { get; set; }

        /// <summary>
        /// Gets the current combined triggers using the list from the
        /// <see cref="TriggerList"/> property.
        /// </summary>
        [XmlIgnore]
        public TriggerType Current
        {
            get
            {
                // Return the triggers if they have already been combined
                // by checking if they are not equal to the default value
                if (_triggers != TriggerType.None)
                {
                    return _triggers;
                }

                if (TriggerList != null && TriggerList.Count > 0)
                {
                    foreach (TriggerType trigger in TriggerList)
                    {
                        _triggers |= trigger;
                    }
                }

                return _triggers;
            }
        }
    }
}
