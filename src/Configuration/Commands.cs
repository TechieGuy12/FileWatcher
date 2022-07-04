using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace TE.FileWatcher.Configuration
{
    /// <summary>
    /// The commands to run when a change is detected.
    /// </summary>
    [XmlRoot("commands")]
    public class Commands
    {
        /// <summary>
        /// Gets or sets the list of actions to perform.
        /// </summary>
        [XmlElement("command")]
        public Collection<Command>? CommandList { get; set; }

        /// <summary>
        /// Runs all the commands for the watch.
        /// </summary>
        /// <param name="watchPath">
        /// The watch path.
        /// </param>
        /// <param name="fullPath">
        /// The full path to the changed file or folder.
        /// </param>
        public void Run(TriggerType trigger, string watchPath, string fullPath)
        {
            if (CommandList == null || CommandList.Count <= 0)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(watchPath) || string.IsNullOrWhiteSpace(fullPath))
            {
                return;
            }

            foreach (Command command in CommandList)
            {
                command.Run(watchPath, fullPath, trigger);
            }
        }
    }
}
