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
    public class Steps : IRunnable
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

        public void Initialize()
        {
            if (StepList == null || StepList.Count == 0)
            {
                return; 
            }

            foreach (Step step in StepList)
            {
                step.Initialize();
                step.SetNeedSteps(StepList);
            }

            HasCompleted = false;
            IsInitialized = true;
        }
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

            Logger.WriteLine($"Starting to run {StepList.Count} step(s).");
            foreach (Step step in StepList)
            {                                
                if (!step.IsInitialized)
                {
                    Logger.WriteLine($"Initialize step {step.Id} - start.");
                    step.Initialize();
                    step.Completed += OnCompleted;
                    Logger.WriteLine($"Initialize step {step.Id} - done.");
                }
                    
                Task.Run(() => { step.Run(_change, _trigger); });
            }
        }

        public virtual void OnStarted(object? sender, TaskEventArgs e)
        {
            Started?.Invoke(this, e);
        }

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
