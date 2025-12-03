using System.Collections.ObjectModel;
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
        // Thread-safe lazy initialization of combined triggers
        private Lazy<TriggerType> _combinedTriggers;

        /// <summary>
        /// Initializes a new instance of the <see cref="Triggers"/> class.
        /// </summary>
        public Triggers()
        {
            _combinedTriggers = new Lazy<TriggerType>(CombineTriggers, LazyThreadSafetyMode.ExecutionAndPublication);
        }

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
                return _combinedTriggers.Value;
            }
        }

        /// <summary>
        /// Combines all triggers in the trigger list into a single flag value.
        /// </summary>
        /// <returns>The combined trigger flags.</returns>
        private TriggerType CombineTriggers()
        {
            if (TriggerList == null || TriggerList.Count == 0)
            {
                return TriggerType.None;
            }

            TriggerType result = TriggerType.None;
            foreach (TriggerType trigger in TriggerList)
            {
                result |= trigger;
            }

            return result;
        }
    }
}
