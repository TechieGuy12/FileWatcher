using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace TE.FileWatcher.Configuration
{
    /// <summary>
    /// Contains information about all actions for a watch.
    /// </summary>
    [XmlRoot("actions")]
    public class Actions : HasVariablesBase
    {
        /// <summary>
        /// Gets or sets the list of actions to perform.
        /// </summary>
        [XmlElement("action")]
        public Collection<Action>? ActionList { get; set; }

        /// <summary>
        /// Add the variables list to the dependent objects.
        /// </summary>
        private void AddVariables()
        {
            if (ActionList != null)
            {
                Parallel.ForEach(ActionList, (action) =>
                {
                    action.Variables ??= new Variables();
                    action.Variables.Add(Variables?.AllVariables);
                });
            }
        }

        /// <summary>
        /// Runs all the actions for the watch.
        /// </summary>
        /// <param name="change">
        /// Information about the change.
        /// </param>
        public void Run(TriggerType trigger, ChangeInfo change)
        {
            if (trigger == TriggerType.Delete)
            {
                return;
            }

            if (ActionList == null || ActionList.Count <= 0)
            {
                return;
            }

            AddVariables();

            foreach (Action action in ActionList)
            {
                action.Run(change, trigger);
            }
        }
    }
}
