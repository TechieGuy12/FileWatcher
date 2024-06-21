using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using System.Diagnostics.Eventing.Reader;
using TE.FileWatcher.Log;

namespace TE.FileWatcher.Configuration
{
    /// <summary>
    /// Contains information about all the steps of a workflow.
    /// </summary>
    [XmlRoot("steps")]
    public class Steps : HasVariablesBase, IRunnable
    {
        private ChangeInfo? _change;

        private TriggerType _trigger;

        /// <summary>
        /// The event for the completion of the steps.
        /// </summary>
        public event CompletedEventHandler? Completed;

        /// <summary>
        /// The event for the start of the steps.
        /// </summary>
        public event StartedEventHandler? Started;

        /// <summary>
        /// Gets or sets the collection of steps to execute.
        /// </summary>
        [XmlElement("step")]
        public Collection<Step>? StepList {  get; set; }

        /// <summary>
        /// Gets the flag indicating all steps have completed running.
        /// </summary>
        [XmlIgnore]
        public bool HasCompleted { get; private set; }

        /// <summary>
        /// Gets the flag indicating the task has been initialized.
        /// </summary>
        [XmlIgnore]
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Add the workflow variable list to the dependent tasks.
        /// </summary>
        private void AddDependentVariables()
        {
            if (StepList != null)
            {
                Parallel.ForEach(StepList, (step) =>
                {
                    step.AddVariables(Variables);
                });
            }
        }

        /// <summary>
        /// Initializes all the steps.
        /// </summary>
        public void Initialize()
        {
            if (StepList == null || StepList.Count == 0)
            {
                return; 
            }

            AddDependentVariables();

            // Initialize each step, and also set any required steps that are
            // needed to be run for any step
            foreach (Step step in StepList)
            {
                step.Initialize();
                step.SetNeedSteps(StepList);
            }

            HasCompleted = false;
            IsInitialized = true;
        }

        /// <summary>
        /// Run the steps.
        /// </summary>
        /// <param name="change">
        /// Information about the change.
        /// </param>
        /// <param name="trigger">
        /// The trigger that caused the change.
        /// </param>
        public void Run(ChangeInfo change, TriggerType trigger)
        {
            if (StepList == null || StepList.Count <= 0)
            {
                return;
            }

            if (!IsInitialized)
            {
                Initialize();
            }

            _change = change;
            _trigger = trigger;

            Logger.WriteLine($"Starting to run {StepList.Count} step(s).", LogLevel.DEBUG);
            foreach (Step step in StepList)
            {                                
                if (!step.IsInitialized)
                {
                    step.Initialize();
                }

                step.Completed += OnCompleted;
                Task.Run(() => { step.Run(_change, _trigger); });
            }
        }

        /// <summary>
        /// Raised when the steps have started.
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
        /// Raised when a step has completed.
        /// </summary>
        /// <param name="sender">
        /// The object that raised the event.
        /// </param>
        /// <param name="e">
        /// Information about the event.
        /// </param>
        public virtual void OnCompleted(object? sender, TaskEventArgs e)
        {
            if (StepList == null || StepList.Count <= 0)
            {
                return;
            }

            HasCompleted = StepList.All(s => s.HasCompleted);
            if (HasCompleted)
            {
                // Once all steps have been completed, reset the steps for the
                // next workflow run
                foreach (Step step in StepList)
                {
                    step.Reset();
                }
                
                Completed?.Invoke(this, new TaskEventArgs(true, null, "All steps completed."));
            }
        }
    }
}
