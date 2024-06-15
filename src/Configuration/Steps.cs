using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using System.Diagnostics.Eventing.Reader;

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
        /// Gets or sets the collection of steps to execute.
        /// </summary>
        [XmlElement("step")]
        public Collection<Step>? StepList {  get; set; }

        /// <summary>
        /// A flag indicating the steps were previously initialized.
        /// </summary>
        private bool isInitialized = false;

        private Step? GetNeedStep(string id)
        {
            if (StepList == null || StepList.Count <= 0)
            {
                return null;
            }

            foreach (Step step in StepList)
            {
                if (step.Id == id)
                {
                    return step;
                }
            }

            return null;
        }

        public void Initialize()
        {
            if (StepList == null || StepList.Count == 0)
            {
                return; 
            }

            foreach(Step step in StepList)
            {
                if (step.Needs != null && step.Needs.Length > 0)
                {
                    for (int i = 0; i < step.Needs.Length; i++)
                    {
                        Step? neededStep = GetNeedStep(step.Needs[i]);
                        if (neededStep != null) 
                        {
                            step.ConnectToNeededStep(neededStep);
                        }
                    }
                }
            }

            isInitialized = true;
        }

        public void Run(ChangeInfo change, TriggerType trigger)
        {
            if (StepList == null || StepList.Count <= 0)
            {
                return;
            }

            if (!isInitialized)
            {
                Initialize();
            }

            _change = change;
            _trigger = trigger;

            foreach (Step step in StepList)
            {
                if (!step.HasCompleted)
                {
                    step.Completed += OnStepCompleted;

                    if (!step.IsInitialized)
                    {
                        step.Initialize();
                    }

                    if (step.CanRun && !step.IsRunning)
                    {
                        Task.Run(() => { step.Run(_change, _trigger); });
                    }
                }
                else
                {
                    step.Reset();
                }
            }
        }

        public void OnStepCompleted(object? sender, TaskEventArgs e)
        {
            if (_change == null)
            {
                return;
            }

            Run(_change, _trigger);
        }
    }
}
