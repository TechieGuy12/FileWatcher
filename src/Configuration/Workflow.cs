using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TE.FileWatcher.Log;

namespace TE.FileWatcher.Configuration
{
    /// <summary>
    /// Contains all the information about a workflow.
    /// </summary>
    [XmlRoot("workflow")]
    public class Workflow : RunnableBase
    {
        /// <summary>
        /// Gets or sets all the steps for the workflow.
        /// </summary>
        [XmlElement("steps")]
        public Steps? Steps { get; set; }

        public override void Run(ChangeInfo change, TriggerType trigger)
        {
            try
            {
                base.Run(change, trigger);
            }
            catch (ArgumentNullException e)
            {
                Logger.WriteLine(e.Message);
                return;
            }
            catch (InvalidOperationException e)
            {
                Logger.WriteLine(e.Message);
                return;
            }
            catch (FileWatcherTriggerNotMatchException)
            {
                return;
            }

            if (Steps == null)
            {
                return;
            }

            // Call the steps, but change the trigger to "Step" as the trigger
            // validation takes place in this workflow and not in the subsequent
            // jobs as it does with the non-workflow configuration
            Steps.Run(change, TriggerType.Step);
        }
    }
}
