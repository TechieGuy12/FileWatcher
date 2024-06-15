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
    public class Step : HasNeedsBase, IRunnable
    {
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

        public void Run(ChangeInfo change, TriggerType trigger)
        {
            Logger.WriteLine($"Running step: {Id}");
            OnStarted(new TaskEventArgs(true, Id, $"{Id} step started."));
            IsRunning = true;

            Action?.Run(change, trigger);             
            Command?.Run(change, trigger);
            Notification?.Run(change, trigger);

            IsRunning = false;
            OnCompleted(new TaskEventArgs(true, Id, $"{Id} step completed."));            
        }

        public void ConnectToNeededStep(Step neededStep)
        {
            neededStep.Completed += OnNeedsCompleted;
        }
    }
}
