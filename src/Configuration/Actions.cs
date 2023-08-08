using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace TE.FileWatcher.Configuration
{
    /// <summary>
    /// Contains information about all actions for a watch.
    /// </summary>
    [XmlRoot("actions")]
    public class Actions
    {
        /// <summary>
        /// Gets or sets the list of actions to perform.
        /// </summary>
        [XmlElement("action")]
        public Collection<Action>? ActionList { get; set; }

        /// <summary>
        /// Runs all the actions for the watch.
        /// </summary>
        /// <param name="change">
        /// Information about the change.
        /// </param>
        public void Run(TriggerType trigger, ChangeInfo change)
        {
            if (ActionList == null || ActionList.Count <= 0)
            {
                return;
            }

            foreach (Action action in ActionList)
            {
                action.Run(change, trigger);
            }
        }
    }
}
