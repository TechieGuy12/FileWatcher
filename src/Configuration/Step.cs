using System.Collections.ObjectModel;
using System.Xml.Serialization;
using TE.FileWatcher.Log;

namespace TE.FileWatcher.Configuration
{
    /// <summary>
    /// Contains all the information for a workflow step.
    /// </summary>
    public class Step : HasNeedsBase, IRunnable
    {
        private ChangeInfo? _change;

        private TriggerType _trigger = default;

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
        /// Initializes an instance of the <see cref="Step"/> class.
        /// </summary>
        public Step()
        {
            Id = string.Empty;
        }

        /// <summary>
        /// Sets up the needed steps for this step.
        /// </summary>
        /// <param name="steps">
        /// The needed steps that are required to be completed before this step
        /// can be run.
        /// </param>
        public void SetNeedSteps(Collection<Step> steps)
        {
            if (string.IsNullOrEmpty(Id) || steps == null || steps.Count <= 0)
            {
                return;
            }

            if (Needs == null || Needs.Length <= 0)
            {
                return;
            }

            for (int i = 0; i < Needs.Length; i++)
            {
                Step? needStep = steps
                    .Where(w => w.Id != null)
                    .FirstOrDefault(w => w.Id == Needs[i]);
                if (needStep != null)
                {
                    Logger.WriteLine($"{Id} reliant on {needStep.Id}.", LogLevel.DEBUG);
                    SetNeed(needStep);
                }
            }
        }

        /// <summary>
        /// Run the step.
        /// </summary>
        /// <param name="change">
        /// Information about the change.
        /// </param>
        /// <param name="trigger">
        /// The trigger that caused the change.
        /// </param>
        public override void Run(ChangeInfo change, TriggerType trigger)
        {
            _change = change;
            _trigger = trigger;

            Logger.WriteLine($"{Id}: CanRun: {CanRun}, Running: {IsRunning}, Trigger: {trigger}", LogLevel.DEBUG);

            // If the step can't be run or is running currently, then don't
            // run the step
            if (!CanRun || IsRunning)
            {
                return;
            }

            Logger.WriteLine($"Running step: {Id}", LogLevel.DEBUG);
            OnStarted(this, new TaskEventArgs(true, Id, $"{Id} step started."));
            IsRunning = true;

            Action?.Run(change, trigger);
            Command?.Run(change, trigger);
            Notification?.Run(change, trigger);

            IsRunning = false;
            OnCompleted(this, new TaskEventArgs(true, Id, $"{Id} step completed."));            
        }

        public override void OnNeedsCompleted(object? sender, TaskEventArgs e)
        {
            base.OnNeedsCompleted(sender, e);

            if (_change != null)
            {
                Run(_change, _trigger);
            }
        }
    }
}
