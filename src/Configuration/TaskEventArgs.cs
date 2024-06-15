using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TE.FileWatcher.Configuration
{
    /// <summary>
    /// Contains the event arguments when a job has completed execution.
    /// </summary>
    public class TaskEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the result of the job execution. True if the job
        /// completed successfully; otherwise false
        /// </summary>
        public bool Result { get; set; }

        /// <summary>
        /// Gets or sets the message associated to the job execution.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the id of the job that was just executed.
        /// </summary>
        public string? Id { get; set; }


        /// <summary>
        /// Initialize the <see cref="TaskEventArgs"/> when provided with the
        /// execution result, the name and the result message.
        /// </summary>
        /// <param name="result">
        /// The result of the job execution.
        /// </param>
        /// <param name="name">
        /// The name of the job.
        /// </param>
        /// <param name="message">
        /// The message associated with the result.
        /// </param>
        public TaskEventArgs(bool result, string? id, string message)
        {
            Result = result;
            Message = message;
            Id = id;
        }
    }
}
