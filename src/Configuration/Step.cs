using System.Collections.ObjectModel;
using System.Xml.Serialization;
using TE.FileWatcher.Log;

namespace TE.FileWatcher.Configuration
{
    /// <summary>
    /// Contains all the information for a workflow step.
    /// </summary>
    public class Step : HasNeedsBase
    {
        private ChangeInfo? _change;

        private TriggerType _trigger = default;
        
        // Track the correlation ID being processed to detect state pollution
        private Guid? _processingCorrelationId = null;

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
        /// Add the variables list to the dependent objects.
        /// </summary>
        private void AddVariables()
        {
            if (Action != null)
            {
                Action.Variables ??= new Variables();
                Action.Variables.Add(Variables?.AllVariables);
            }

            if (Command != null)
            {
                Command.Variables ??= new Variables();
                Command.Variables.Add(Variables?.AllVariables);
            }

            if (Notification != null)
            {
                Notification.Variables ??= new Variables();
                Notification.Variables.Add(Variables?.AllVariables);
            }
        }

        /// <summary>
        /// Initializes the step.
        /// </summary>
        public override void Initialize()
        {
            if (!IsInitialized)
            {
                AddVariables();
            }

            // Clear stale change state so OnNeedsCompleted doesn't use a previous run's change
            _change = null;
            _trigger = default;
            _processingCorrelationId = null;

            base.Initialize();
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
                    Logger.WriteLine($"{Id}: Needs {needStep.Id}. (Step.SetNeedSteps)", LogLevel.DEBUG);
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
            // Detect if this step is being reused while still processing another file
            if (_processingCorrelationId.HasValue && _processingCorrelationId.Value != change.CorrelationId)
            {
                Logger.WriteLine(
                    $"[{change.CorrelationId}] WARNING: Step '{Id}' is being reused while still processing [{_processingCorrelationId.Value}]. This may cause state pollution!",
                    LogLevel.WARNING);
            }
            
            _change = change;
            _trigger = trigger;
            _processingCorrelationId = change.CorrelationId;

            Logger.WriteLine(
                $"[{change.CorrelationId}] {Id}: CanRun: {CanRun}, Running: {IsRunning}, Trigger: {trigger} (Step.Run)",
                LogLevel.DEBUG);

            // If the step can't be run or is running currently, then don't
            // run the step
            if (!CanRun || IsRunning)
            {
                Logger.WriteLine(
                    $"[{change.CorrelationId}] {Id}: The step cannot run at this time. (Step.Run)",
                    LogLevel.DEBUG);
                return;
            }

            OnStarted(this, new TaskEventArgs(true, Id, $"{Id}: Step started."));
            IsRunning = true;

            Logger.WriteLine($"[{change.CorrelationId}] {Id}: Running the action (if any). (Step.Run)", LogLevel.DEBUG);
            Action?.Run(change, trigger);
            Logger.WriteLine($"[{change.CorrelationId}] {Id}: Running the command (if any). (Step.Run)", LogLevel.DEBUG);
            Command?.Run(change, trigger);
            Logger.WriteLine($"[{change.CorrelationId}] {Id}: Sending the notification (if any). (Step.Run)", LogLevel.DEBUG);
            Notification?.Run(change, trigger);

            IsRunning = false;
            _processingCorrelationId = null; // Clear after completion
            OnCompleted(this, new TaskEventArgs(true, Id, $"{Id}: Step completed."));            
        }

        public override void OnNeedsCompleted(object? sender, TaskEventArgs e)
        {
            Logger.WriteLine(
                $"{Id}: Needed step {e.Id} completed. Checking if this step can run. (Step.OnNeedsCompleted)",
                LogLevel.DEBUG);

            base.OnNeedsCompleted(sender, e);

            if (_change != null)
            {
                // Log correlation ID when running from dependency completion
                Logger.WriteLine(
                    $"[{_change.CorrelationId}] {Id}: Running after dependency completed. (Step.OnNeedsCompleted)",
                    LogLevel.DEBUG);
                Run(_change, _trigger);
            }
        }
    }
}
