using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace TE.FileWatcher.Configuration
{
    /// <summary>
    /// The commands to run when a change is detected.
    /// </summary>
    [XmlRoot("workflows")]
    public class Workflows : IRunnable
    {
        /// <summary>
        /// Gets or sets the list of actions to perform.
        /// </summary>
        [XmlElement("workflow")]
        public Collection<Workflow>? WorkflowList { get; set; }

        /// <summary>
        /// Runs all the commands for the watch.
        /// </summary>
        /// <param name="change">
        /// Information about the change.
        /// </param>
        public void Run(ChangeInfo change, TriggerType trigger)
        {
            if (WorkflowList == null || WorkflowList.Count <= 0)
            {
                return;
            }

            foreach (Workflow workflow in WorkflowList)
            {
                workflow.Run(change, trigger);
            }
        }
    }
}
