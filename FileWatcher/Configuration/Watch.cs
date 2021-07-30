using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TE.FileWatcher.Logging;

namespace TE.FileWatcher.Configuration
{
    /// <summary>
    /// The watch element in the XML file.
    /// </summary>
    public class Watch
    {
        // The background worker that processes the file/folder changes
        private BackgroundWorker _worker;

        // The queue that will contain the changes
        private ConcurrentQueue<ChangeInfo> _queue;

        /// <summary>
        /// Gets or sets the path of the watch.
        /// </summary>
        [XmlElement("path")]
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the exclusions
        /// </summary>
        [XmlElement("exclusions")]
        public Exclusions.Exclusions Exclusions { get; set; }

        /// <summary>
        /// Gets or sets the notifications for the watch.
        /// </summary>
        [XmlElement("notifications")]
        public Notifications.Notifications Notifications { get; set; }

        /// <summary>
        /// Gets or sets the actions for the watch.
        /// </summary>
        [XmlElement("actions")]
        public Actions.Actions Actions { get; set; }

        /// <summary>
        /// Initializes an instance of the <see cref="Watch"/> class.
        /// </summary>
        public Watch()
        {
            // Initializes the queue
            _queue = new ConcurrentQueue<ChangeInfo>();

            // Initialize the background worker
            _worker = new BackgroundWorker();
            _worker.WorkerSupportsCancellation = false;
            _worker.DoWork += DoWork;            
        }

        /// <summary>
        /// Gets the string value for the message type.
        /// </summary>
        /// <param name="trigger">
        /// The notification trigger.
        /// </param>
        /// <returns>
        /// The string value for the message type, otherwise <c>null</c>.
        /// </returns>
        private string GetMessageTypeString(TriggerType trigger)
        {
            string messageType = null;
            switch (trigger)
            {
                case TriggerType.Create:
                    messageType = "Created";
                    break;
                case TriggerType.Change:
                    messageType = "Changed";
                    break;
                case TriggerType.Delete:
                    messageType = "Deleted";
                    break;
                case TriggerType.Rename:
                    messageType = "Renamed";
                    break;
            }

            return messageType;
        }

        /// <summary>
        /// Process the changes in a background worker thread.
        /// </summary>
        /// <param name="sender">
        /// The object associated with this event.
        /// </param>
        /// <param name="e">
        /// Arguments associated with the background worker.
        /// </param>
        private void DoWork(object sender, DoWorkEventArgs e)
        {
            if (_queue.IsEmpty)
            {
                Thread.Sleep(100);
            }

            while (!_queue.IsEmpty)
            {

                if (_queue.TryDequeue(out ChangeInfo change))
                {
                    if (Exclusions != null)
                    {
                        // If the file or folder is in the exclude list, then don't take
                        // any further actions
                        if (Exclusions.Exclude(Path, change.Name, change.FullPath))
                        {
                            return;
                        }
                    }

                    // Send the notifications
                    string messageType = GetMessageTypeString(change.Trigger);
                    if (!string.IsNullOrWhiteSpace(messageType))
                    {
                        Notifications?.Send(change.Trigger, $"{messageType}: {change.FullPath}");
                    }

                    // Only run the actions if a file wasn't deleted, as the file no
                    // longer exists so no action can be taken on the file
                    if (change.Trigger != TriggerType.Delete)
                    {
                        Actions?.Run(change.Trigger, Path, change.FullPath);
                    }
                }
            }
        }

        /// <summary>
        /// Processes the file or folder change.
        /// </summary>
        /// <param name="trigger">
        /// The type of change.
        /// </param>
        /// <param name="name">
        /// The name of the file or folder.
        /// </param>
        /// <param name="fullPath">
        /// The full path of the file or folder.
        /// </param>
        public void ProcessChange(ChangeInfo change)
        {
            if (change == null)
            {
                return;
            }

            _queue.Enqueue(change);
            if (!_worker.IsBusy)
            {
                _worker.RunWorkerAsync();
            }
        }
    }
}
