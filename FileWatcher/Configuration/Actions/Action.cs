using System;
using System.Collections.Generic;
using IO = System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TE.FileWatcher.Logging;
using TE.FileWatcher.FileSystem;

namespace TE.FileWatcher.Configuration.Actions
{
    /// <summary>
    /// The Action to perform during a watch event.
    /// </summary>
    public class Action : RunnableBase
    {
        // The placeholders for the destination path
        private Dictionary<string, string> _destinationPlaceholders 
            = new Dictionary<string, string>();

        // The placeholders for the source path
        private Dictionary<string, string> _sourcePlaceholders
            = new Dictionary<string, string>();

        /// <summary>
        /// The type of action to perform.
        /// </summary>
        [Serializable]
        public enum ActionType
        {
            /// <summary>
            /// Copy a file.
            /// </summary>
            Copy,
            /// <summary>
            /// Move a file.
            /// </summary>
            Move,
            /// <summary>
            /// Delete a file.
            /// </summary>
            Delete
        }

        /// <summary>
        /// Gets or sets the type of action to perform.
        /// </summary>
        [XmlElement("type")]
        public ActionType Type { get; set; }

        /// <summary>
        /// Gets or sets the source of the action.
        /// </summary>
        [XmlElement("source")]
        public string Source { get; set; } = PLACEHOLDER_FULLPATH;

        /// <summary>
        /// Gets or sets the triggers of the action.
        /// </summary>
        [XmlElement("triggers")]
        public Triggers Triggers { get; set; } = new Triggers();

        /// <summary>
        /// Gets or sets the destination of the action.
        /// </summary>
        [XmlElement("destination")]
        public string Destination { get; set; }

        /// <summary>
        /// Gets or sets the verify flag.
        /// </summary>
        [XmlElement(ElementName = "verify", DataType = "boolean")]
        public bool Verify { get; set; }

        /// <summary>
        /// Runs the action.
        /// </summary>
        /// <param name="watchPath">
        /// The watch path.
        /// </param>
        /// <param name="fullPath">
        /// The full path to the changed file or folder.
        /// </param>
        /// <param name="trigger">
        /// The trigger for the action.
        /// </param>
        public override void Run(string watchPath, string fullPath, TriggerType trigger)
        {
            if (string.IsNullOrWhiteSpace(watchPath) || string.IsNullOrWhiteSpace(fullPath))
            {
                return;
            }

            if (Triggers == null || Triggers.TriggerList == null)
            {
                return;
            }

            if (Triggers.TriggerList.Count <= 0 || !Triggers.Current.HasFlag(trigger))
            {
                return;
            }

            string source = GetSource(watchPath, fullPath);
            string destination = GetDestination(watchPath, fullPath);

            try
            {
                switch (Type)
                {
                    case ActionType.Copy:
                        if (File.IsValid(source))
                        {
                            File.Copy(source, destination, Verify);
                            Logger.WriteLine($"Copied {source} to {destination}.");
                        }
                        break;
                    case ActionType.Move:
                        if (File.IsValid(source))
                        {
                            File.Move(source, destination, Verify);
                            Logger.WriteLine($"Moved {source} to {destination}.");
                        }
                        break;
                    case ActionType.Delete:
                        if (File.IsValid(source))
                        {
                            File.Delete(source);
                            Logger.WriteLine($"Deleted {source}.");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                string message = (ex.InnerException == null) ? ex.Message : ex.InnerException.Message;
                Logger.WriteLine($"Could not {Type.ToString().ToLower()} file {source}. Reason: {message}");
                return;
            }
        }

        /// <summary>
        /// Gets the destination value by replacing any placeholders with the
        /// actual string values.
        /// </summary>
        /// <param name="watchPath">
        /// The watch path.
        /// </param>
        /// <param name="fullPath">
        /// The full path of the changed file.
        /// </param>
        /// <returns>
        /// The destination string value.
        /// </returns>
        private string GetDestination(string watchPath, string fullPath)
        {
            if (string.IsNullOrWhiteSpace(Destination))
            {
                return null;
            }

            return ReplacePlaceholders(Destination, watchPath, fullPath);
        }

        /// <summary>
        /// Gets the source value by replacing any placeholders with the actual
        /// string values.
        /// </summary>
        /// <param name="watchPath">
        /// The watch path.
        /// </param>
        /// <param name="fullPath">
        /// The full path of the changed file.
        /// </param>
        /// <returns>
        /// The source string value.
        /// </returns>
        private string GetSource(string watchPath, string fullPath)
        {
            if (string.IsNullOrWhiteSpace(Source))
            {
                return null;
            }

            return ReplacePlaceholders(Source, watchPath, fullPath);
        }
    }
}
