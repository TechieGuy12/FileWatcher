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

        [XmlIgnore]
        public bool HasCompleted { get; private set; }

        /// <summary>
        /// Gets the flag indicating the task has been initialized.
        /// </summary>
        [XmlIgnore]
        public bool IsInitialized { get; private set; }

        public void Initialize()
        {            
            HasCompleted = false;
            IsInitialized = true;
        }

        public override void Run(ChangeInfo change, TriggerType trigger)
        {
            try
            {
                base.Run(change, trigger);

                if (!IsInitialized)
                {
                    Initialize();
                }
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

            Logger.WriteLine($"Running steps.");
            Steps.Initialize();
            // Call the steps, but change the trigger to "Step" as the trigger
            // validation takes place in this workflow and not in the subsequent
            // jobs as it does with the non-workflow configuration
            Steps.Completed += OnStepsCompleted;
            Steps.Run(change, TriggerType.Step);
            while (!Steps.HasCompleted) { }
        }

        public void OnStepsCompleted(object? sender, TaskEventArgs e)
        {
            if (Steps == null)
            {
                return;
            }

            HasCompleted = Steps.HasCompleted;
            if (HasCompleted)
            {
                Steps.Initialize();
                base.OnCompleted(this, new TaskEventArgs(true, null, "All steps have completed."));
            }
        }
    }
}
