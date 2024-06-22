using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace TE.FileWatcher.Configuration
{
    /// <summary>
    /// The commands to run when a change is detected.
    /// </summary>
    [XmlRoot("commands")]
    public class Commands: HasVariablesBase
    {
        /// <summary>
        /// Gets or sets the list of actions to perform.
        /// </summary>
        [XmlElement("command")]
        public Collection<Command>? CommandList { get; set; }


        /// <summary>
        /// Add the variables list to the dependent objects.
        /// </summary>
        private void AddVariables()
        {
            if (CommandList != null)
            {
                Parallel.ForEach(CommandList, (command) =>
                {
                    command.Variables ??= new Variables();
                    command.Variables.Add(Variables?.AllVariables);
                });
            }
        }

        /// <summary>
        /// Runs all the commands for the watch.
        /// </summary>
        /// <param name="change">
        /// Information about the change.
        /// </param>
        public void Run(TriggerType trigger, ChangeInfo change)
        {
            if (CommandList == null || CommandList.Count <= 0)
            {
                return;
            }

            AddVariables();

            foreach (Command command in CommandList)
            {
                command.Run(change, trigger);
            }
        }
    }
}
