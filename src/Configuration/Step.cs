using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TE.FileWatcher.Log;

namespace TE.FileWatcher.Configuration
{
    /// <summary>
    /// Contains all the information for a workflow step.
    /// </summary>
    public class Step : IRunnable
    {
        /// <summary>
        /// The delegate for the step completion event.
        /// </summary>
        /// <param name="sender">
        /// The object that invoked the event.
        /// </param>
        /// <param name="e">
        /// The step event arguments.
        /// </param>
        public delegate void StepCompletedEventHandler(object? sender, StepEventArgs e);

        /// <summary>
        /// The delegate for the step started event.
        /// </summary>
        /// <param name="sender">
        /// The object that invoked the event.
        /// </param>
        /// <param name="e">
        /// The step event arguments.
        /// </param>
        public delegate void StepStartedEventHandler(object? sender, StepEventArgs e);

        /// <summary>
        /// The event for the completion of the step.
        /// </summary>
        public event StepCompletedEventHandler? StepCompleted;

        /// <summary>
        /// The event for the start of the step.
        /// </summary>
        public event StepStartedEventHandler? StepStarted;

        /// <summary>
        /// Gets or sets the id of the step.
        /// </summary>
        [XmlElement(ElementName = "id", IsNullable = false)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the action, if any, that is associated with the step.
        /// </summary>
        [XmlElement(ElementName = "action", IsNullable = true)]
        public Action? Action { get; set; }

        /// <summary>
        /// Gets or sets the command, if any, that is associated with the step.
        /// </summary>
        [XmlElement(ElementName = "command", IsNullable = true)]
        public Command? Command { get; set; }

        /// <summary>
        /// Gets or sets the notification, if any, that is associated with the
        /// step.
        /// </summary>
        [XmlElement(ElementName = "notification", IsNullable = true)]
        public Notification? Notification { get; set; }

        /// <summary>
        /// Gets or sets the list of steps that need to be completed before this
        /// step is run.
        /// </summary>
        [XmlArray(ElementName = "needs", IsNullable = true)]
        [XmlArrayItem(ElementName = "need", IsNullable = true)]
        public string[]? Needs { get; set; }

        /// <summary>
        /// The number of pre-requisite jobs that have completed.
        /// </summary>
        private int needsJobsCompleted;

        /// <summary>
        /// Gets the flag indicating the step has completed running.
        /// </summary>
        public bool HasCompleted { get; private set; }

        /// <summary>
        /// Initializes an instance of the <see cref="Step"/> class.
        /// </summary>
        public Step()
        {
            Id = string.Empty;
            Initialize();
        }

        private void Initialize()
        {
            HasCompleted = false;
            needsJobsCompleted = 0;
        }

        /// <summary>
        /// Invoke the step completed event.
        /// </summary>
        /// <param name="e">
        /// The step completed arguments.
        /// </param>
        protected virtual void OnCompleted(StepEventArgs e)
        {
            HasCompleted = true;
            StepCompleted?.Invoke(this, e);            
        }

        /// <summary>
        /// Invoke the step completed event.
        /// </summary>
        /// <param name="e">
        /// The step completed arguments.
        /// </param>
        protected virtual void OnStarted(StepEventArgs e)
        {
            StepStarted?.Invoke(this, e);
        }

        /// <summary>
        /// Resets the step for the next workflow run.
        /// </summary>
        public void Reset()
        {
            Initialize();
        }

        public void Run(ChangeInfo change, TriggerType trigger)
        {
            Logger.WriteLine($"Running step: {Id}");
            OnStarted(new StepEventArgs(true, Id, $"{Id} step started."));

            if (Action != null)
            {
                Action.Run(change, trigger);
            }

            if (Command != null)
            {                
                Command.Run(change, trigger);
            }

            if (Notification != null)
            {
                Notification.Run(change, trigger);
            }

            OnCompleted(new StepEventArgs(true, Id, $"{Id} step completed."));
        }

        public void OnNeedsStepCompleted(object? sender, StepEventArgs e)
        {
            if (Needs != null && needsJobsCompleted == Needs.Length)
            {
                Logger.WriteLine($"{e.Id} has finished running.");
                needsJobsCompleted = 0;
            }
            else
            {
                needsJobsCompleted++;
            }
        }

        public void ConnectToNeededStep(Step neededStep)
        {
            neededStep.StepCompleted += OnNeedsStepCompleted;
        }
    }
}
