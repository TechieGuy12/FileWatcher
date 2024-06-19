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

        /// <summary>
        /// Initializes the workflow.
        /// </summary>
        public void Initialize()
        {            
            HasCompleted = false;
            IsInitialized = true;
        }

        /// <summary>
        /// Runs the workflow.
        /// </summary>
        /// <param name="change">
        /// Information about the change that happened.
        /// </param>
        /// <param name="trigger">
        /// The trigger that caused the change.
        /// </param>
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

            Logger.WriteLine($"Running steps.", LogLevel.DEBUG);
            Steps.Initialize();
            Steps.Completed += OnStepsCompleted;

            // Call the steps, but change the trigger to "Step" as the trigger
            // validation takes place in this workflow and not in the subsequent
            // jobs as it does with the non-workflow configuration
            Steps.Run(change, TriggerType.Step);

            while (!Steps.HasCompleted) { }
        }

        /// <summary>
        /// Raised when all the steps associated with the workflow have
        /// completed.
        /// </summary>
        /// <param name="sender">
        /// The object that raised the event.
        /// </param>
        /// <param name="e">
        /// Information about the completed steps.
        /// </param>
        public void OnStepsCompleted(object? sender, TaskEventArgs e)
        {
            if (Steps == null)
            {
                return;
            }

            HasCompleted = Steps.HasCompleted;
            if (HasCompleted)
            {
                Logger.WriteLine("All steps completed.", LogLevel.DEBUG);
                Steps.Initialize();
                base.OnCompleted(this, new TaskEventArgs(true, null, "All steps have completed."));
            }
        }
    }
}
