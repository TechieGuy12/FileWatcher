using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TE.FileWatcher.Configuration
{
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

    internal interface IRunnable
    {
        /// <summary>
        /// The event for the completion of the task.
        /// </summary>
        event CompletedEventHandler? Completed;

        /// <summary>
        /// The event for the start of the task.
        /// </summary>
        event StartedEventHandler? Started;

        void Run(ChangeInfo change, TriggerType trigger);

        void OnCompleted(object? sender, TaskEventArgs e);

        void OnStarted(object? sender, TaskEventArgs e);
    }
}
