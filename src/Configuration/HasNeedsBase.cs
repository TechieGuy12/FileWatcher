using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TE.FileWatcher.Log;

namespace TE.FileWatcher.Configuration
{
    public abstract class HasNeedsBase : IRunnable
    {
        /// <summary>
        /// The event for the completion of the task.
        /// </summary>
        public event CompletedEventHandler? Completed;

        /// <summary>
        /// The event for the start of the task.
        /// </summary>
        public event StartedEventHandler? Started;

        /// <summary>
        /// The list of needed tasks to be completed before this task can run.
        /// </summary>
        protected List<HasNeedsBase>? _needs;

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
        public bool CanRun
        {
            get
            {
                // If there are no needs, then return true to indicate the task
                // can be run
                if (_needs == null)
                {
                    return true;
                }
                else
                {
                    // Return the value if all needs have been completed to
                    // indicate the task can be run
                    return _needs.All(n => n.HasCompleted);
                }

            }
        }

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

        public void Initialize()
        {
            HasCompleted = false;
            IsRunning = false;
            IsInitialized = true;
        }

        /// <summary>
        /// Resets the step for the next workflow run.
        /// </summary>
        public void Reset()
        {
            Initialize();
        }

        public abstract void Run(ChangeInfo change, TriggerType trigger);
 
        public void SetNeed(HasNeedsBase Need)
        {
            _needs ??= new List<HasNeedsBase>();
            _needs.Add(Need);
            Need.Completed += OnNeedsCompleted;
        }

        /// <summary>
        /// Invoke the task completed event.
        /// </summary>
        /// <param name="e">
        /// The step completed arguments.
        /// </param>
        public virtual void OnCompleted(object? sender, TaskEventArgs e)
        {
            Logger.WriteLine($"{e.Id} has completed.");
            HasCompleted = true;
            IsRunning = false;
            Completed?.Invoke(this, e);            
        }

        /// <summary>
        /// Invoke the task completed event.
        /// </summary>
        /// <param name="e">
        /// The step completed arguments.
        /// </param>
        public virtual void OnStarted(object? sender, TaskEventArgs e)
        {
            IsRunning = true;
            Started?.Invoke(this, e);
            Logger.WriteLine($"{e.Id} has started.");
        }

        public virtual void OnNeedsCompleted(object? sender, TaskEventArgs e)
        {

        }
    }
}
