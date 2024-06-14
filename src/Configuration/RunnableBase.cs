using System.Text.RegularExpressions;
using System.IO;
using TE.FileWatcher.Log;
using TEFS = TE.FileWatcher.FileSystem;
using System.Xml.Serialization;
using System.Globalization;
using System.Web;
using System;

namespace TE.FileWatcher.Configuration
{
    /// <summary>
    /// A base abstract class for the classes which require execution on the
    /// machine and includes placeholders in the data that need to be replaced.
    /// </summary>
    public abstract class RunnableBase : ItemBase, IRunnable
    {
        /// <summary>
        /// Gets or sets the number of milliseconds to wait before running.
        /// </summary>
        [XmlElement("waitbefore")]
        public int WaitBefore { get; set; }

        /// <summary>
        /// The abstract method to Run.
        /// </summary>
        /// <param name="change">
        /// Information about the change.
        /// </param>
        /// <param name="trigger">
        /// The trigger for the action.
        /// </param>
        public virtual void Run(ChangeInfo change, TriggerType trigger)
        {
            // If the trigger is not a step trigger, then make sure it matches
            // the correct trigger before running the job
            if (trigger != TriggerType.Step)
            {
                if (Triggers == null || Triggers.TriggerList == null)
                {
                    throw new InvalidOperationException("The list of triggers was not provided.");
                }

                if (Triggers.TriggerList.Count <= 0)
                {
                    throw new InvalidOperationException("No triggers were defined.");
                }

                if (!Triggers.Current.HasFlag(trigger))
                {
                    throw new FileWatcherTriggerNotMatchException(
                        "The trigger doesn't match the list of triggers for this watch.");
                }
            }

            Change = change ?? throw new ArgumentNullException(nameof(change));
        }
    }
}
