using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TE.FileWatcher.Log;

namespace TE.FileWatcher.Configuration
{
    public abstract class HasNeedsBase
    {
        /// <summary>
        /// The number of pre-requisite jobs that have completed.
        /// </summary>
        protected int _needsCompleted;

        /// <summary>
        /// The delegate for the copmleted event.
        /// </summary>
        /// <param name="sender">
        /// The object that invoked the event.
        /// </param>
        /// <param name="e">
        /// The step event arguments.
        /// </param>
        public delegate void CompletedEventHandler(object? sender, TaskEventArgs e);

        /// <summary>
        /// The delegate for the started event.
        /// </summary>
        /// <param name="sender">
        /// The object that invoked the event.
        /// </param>
        /// <param name="e">
        /// The step event arguments.
        /// </param>
        public delegate void StartedEventHandler(object? sender, TaskEventArgs e);

        /// <summary>
        /// The event for the completion of the task.
        /// </summary>
        public event CompletedEventHandler? Completed;

        /// <summary>
        /// The event for the start of the task.
        /// </summary>
        public event StartedEventHandler? Started;

        /// <summary>
        /// Gets or sets the list of tasks that need to be completed before this
        /// step is run.
        /// </summary>
        [XmlArray(ElementName = "needs", IsNullable = true)]
        [XmlArrayItem(ElementName = "need", IsNullable = true)]
        public string[]? Needs { get; set; }

        /// <summary>
        /// A flag indicating the current task can run.
        /// </summary>
        [XmlIgnore]
        public bool CanRun { get; protected set; }

        /// <summary>
        /// Gets the flag indicating the task has completed running.
        /// </summary>
        [XmlIgnore]
        public bool HasCompleted { get; protected set; }

        /// <summary>
        /// Gets the flag indicating the task has been initialized.
        /// </summary>
        [XmlIgnore]
        public bool IsInitialized { get; protected set; }

        /// <summary>
        /// Gets the flag indicating the task is currenting running.
        /// </summary>
        [XmlIgnore]
        public bool IsRunning { get; protected set; }

        private void SetCanRunStatus()
        {
            if (Needs == null || _needsCompleted == Needs.Length)
            {
                _needsCompleted = 0;
                CanRun = true;
            }
            else
            {
                _needsCompleted++;
                CanRun = false;
            }
        }

        public void Initialize()
        {
            HasCompleted = false;
            _needsCompleted = 0;
            IsRunning = false;
            SetCanRunStatus();
            IsInitialized = true;
        }

        /// <summary>
        /// Resets the step for the next workflow run.
        /// </summary>
        public void Reset()
        {
            Initialize();
        }

        /// <summary>
        /// Invoke the step completed event.
        /// </summary>
        /// <param name="e">
        /// The step completed arguments.
        /// </param>
        protected virtual void OnCompleted(TaskEventArgs e)
        {
            HasCompleted = true;
            Completed?.Invoke(this, e);
            Logger.WriteLine($"{e.Id} has completed.");
        }

        /// <summary>
        /// Invoke the step completed event.
        /// </summary>
        /// <param name="e">
        /// The step completed arguments.
        /// </param>
        protected virtual void OnStarted(TaskEventArgs e)
        {
            Started?.Invoke(this, e);
            Logger.WriteLine($"{e.Id} has started.");
        }

        public void OnNeedsCompleted(object? sender, TaskEventArgs e)
        {
            SetCanRunStatus();
        }
    }
}
