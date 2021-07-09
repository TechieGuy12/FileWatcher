using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TE.FileWatcher.Logging;

namespace TE.FileWatcher.Configuration.Actions
{
    [XmlRoot("actions")]
    public class Actions
    {
        /// <summary>
        /// Gets or sets the list of actions to perform.
        /// </summary>
        [XmlElement("action")]
        public List<Action> ActionList { get; set; }

        /// <summary>
        /// Runs all the actions for the watch.
        /// </summary>
        /// <param name="watchPath">
        /// The watch path.
        /// </param>
        /// <param name="fullPath">
        /// The full path to the changed file or folder.
        /// </param>
        public void Run(TriggerType trigger, string watchPath, string fullPath)
        {
            if (ActionList?.Count <= 0)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(watchPath) || string.IsNullOrWhiteSpace(fullPath))
            {
                return;
            }
            
            foreach (Action action in ActionList)
            {
                if (action.Triggers?.TriggerList.Count > 0)
                {
                    if (action.Triggers.Current.HasFlag(trigger))
                    {
                        action.Run(watchPath, fullPath);
                    }
                }
            }
        }
    }
}
