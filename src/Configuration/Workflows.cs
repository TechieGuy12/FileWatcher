using System.Collections.ObjectModel;
using System.Xml.Serialization;
using TE.FileWatcher.Log;

namespace TE.FileWatcher.Configuration
{
    /// <summary>
    /// The commands to run when a change is detected.
    /// </summary>
    [XmlRoot("workflows")]
    public class Workflows : IRunnable
    {
        /// <summary>
        /// The event for the completion of the workflows.
        /// </summary>
        public event CompletedEventHandler? Completed;

        /// <summary>
        /// The event for the start of the workflows.
        /// </summary>
        public event StartedEventHandler? Started;

        /// <summary>
        /// Gets or sets the list of actions to perform.
        /// </summary>
        [XmlElement("workflow")]
        public Collection<Workflow>? WorkflowList { get; set; }

        [XmlIgnore]
        public bool HasCompleted { get; private set; }

        /// <summary>
        /// Gets the flag indicating the task has been initialized.
        /// </summary>
        [XmlIgnore]
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Initializes the workflows.
        /// </summary>
        public void Initialize()
        {
            HasCompleted = false;
            IsInitialized = true;
        }

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

            if (!IsInitialized)
            {
                Initialize();
            }

            foreach (Workflow workflow in WorkflowList)
            {                
                workflow.Run(change, trigger);
                workflow.Completed += OnCompleted;
            }
        }

        /// <summary>
        /// Raised when the workflows have started.
        /// </summary>
        /// <param name="sender">
        /// The object that raised the event.
        /// </param>
        /// <param name="e">
        /// Information about the event.
        /// </param>
        public virtual void OnStarted(object? sender, TaskEventArgs e)
        {
            Started?.Invoke(this, e);
        }

        /// <summary>
        /// Raised when a workflow has completed.
        /// </summary>
        /// <param name="sender">
        /// The object that raised the event.
        /// </param>
        /// <param name="e">
        /// Information about the event.
        /// </param>
        public virtual void OnCompleted(object? sender, TaskEventArgs e)
        {            
            if (WorkflowList == null || WorkflowList.Count <= 0)
            {
                return;
            }

            HasCompleted = WorkflowList.All(w => w.HasCompleted);
            if (HasCompleted)
            {
                Logger.WriteLine("All workflows completed.", LogLevel.DEBUG);

                // Once all steps have been completed, reset the steps for the
                // next workflow run
                foreach (Workflow workflow in WorkflowList)
                {
                    workflow.Initialize();
                }

                Initialize();
                Completed?.Invoke(this, e);
            }
        }
    }
}
