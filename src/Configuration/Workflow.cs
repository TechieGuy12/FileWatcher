using System;
using System.Collections.Concurrent;
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
        // Prevent subscribing multiple times
        private bool _stepsSubscribed = false;

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
        /// Add the variables list to the dependent objects.
        /// </summary>
        private void AddVariables()
        {
            if (Steps != null)
            {
                Steps.Variables ??= new Variables();
                Steps.Variables.Add(Variables?.AllVariables);
            }            
        }

        /// <summary>
        /// Initializes the workflow.
        /// </summary>
        public void Initialize()
        {
            if (!IsInitialized)
            {
                AddVariables();
            }

            // Subscribe to Steps.Completed once during initialization
            if (Steps != null && !_stepsSubscribed)
            {
                Steps.Completed += OnStepsCompleted;
                _stepsSubscribed = true;
            }

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
            Logger.WriteLine(
                $"[{change.CorrelationId}] Workflow.Run() started for file: {change.FullPath} (Workflow.Run)",
                LogLevel.DEBUG);
            
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
                Logger.WriteLine($"[{change.CorrelationId}] {e.Message}", LogLevel.ERROR);
                return;
            }
            catch (InvalidOperationException e)
            {
                Logger.WriteLine($"[{change.CorrelationId}] {e.Message}", LogLevel.ERROR);
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

            Logger.WriteLine($"[{change.CorrelationId}] Running steps. (Workflow.Run)", LogLevel.DEBUG);
            Steps.Initialize();

            // Do not subscribe/unsubscribe here; subscription is managed in Initialize()
            // Call the steps, but change the trigger to "Step" as the trigger
            // validation takes place in this workflow and not in the subsequent
            // jobs as it does with the non-workflow configuration
            Steps.Run(change, TriggerType.Step);
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
                Logger.WriteLine("Workflow steps completed. (Workflow.OnStepsCompleted)", LogLevel.DEBUG);
                base.OnCompleted(this, new TaskEventArgs(true, null, "All steps have completed."));
            }
        }
    }
}
