using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using TE.FileWatcher.Log;

namespace TE.FileWatcher.Configuration
{
    /// <summary>
    /// The commands to run when a change is detected.
    /// </summary>
    [XmlRoot("workflows")]
    public class Workflows : HasVariablesBase, IRunnable
    {
        // Prevent subscribing multiple times
        private bool _subscriptionsAdded = false;

        // Track completed workflow count for efficient completion checking
        private int _completedCount = 0;

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
        /// Add the variables list to the dependent objects.
        /// </summary>
        private void AddVariables()
        {
            Logger.WriteLine($"Workflows variable count: {Variables?.AllVariables?.Count}.");
            if (WorkflowList == null)
            {
                return;
            }

            foreach (Workflow workflow in WorkflowList)
            {
                workflow.Variables ??= new Variables();
                workflow.Variables?.Add(Variables?.AllVariables);
            }
        }

        /// <summary>
        /// Initializes the workflows.
        /// </summary>
        public void Initialize()
        {
            if (!IsInitialized)
            {
                AddVariables();
            }

            // Subscribe to workflow completions once during initialization
            if (WorkflowList != null && !_subscriptionsAdded)
            {
                foreach (Workflow workflow in WorkflowList)
                {
                    workflow.Completed += OnCompleted;
                }
                _subscriptionsAdded = true;
            }

            HasCompleted = false;
            _completedCount = 0;
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

            // Reset completion state for this run
            HasCompleted = false;
            _completedCount = 0;

            foreach (Workflow workflow in WorkflowList)
            {
                // Do not subscribe/unsubscribe here; subscriptions are managed in Initialize()
                workflow.Run(change, trigger);
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

            // Increment completed count and check if all workflows are complete
            // This avoids LINQ .All() allocation
            _completedCount++;
            
            if (_completedCount >= WorkflowList.Count)
            {
                HasCompleted = true;
                Logger.WriteLine("All workflows completed. (Workflows.OnCompleted)", LogLevel.DEBUG);

                // Do NOT reset workflows here as they may be in use by other concurrent file processing
                // Workflows.Run() already handles initialization for each new file
                
                Completed?.Invoke(this, e);
            }
        }
    }
}
