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
            }
        }

        public virtual void OnStarted(object? sender, TaskEventArgs e)
        {
            Started?.Invoke(this, e);
        }

        public virtual void OnCompleted(object? sender, TaskEventArgs e)
        {
            Completed?.Invoke(this, e);
        }

        public void OnWorkflowCompleted(object? sender, TaskEventArgs e)
        {
            if (WorkflowList == null || WorkflowList.Count <= 0)
            {
                return;
            }

            HasCompleted = WorkflowList.All(w => w.HasCompleted);
            if (HasCompleted)
            {
                // Once all steps have been completed, reset the steps for the
                // next workflow run
                foreach (Workflow workflow in WorkflowList)
                {
                    workflow.Initialize();
                }

                Initialize();
            }
        }
    }
}
