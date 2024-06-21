using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace TE.FileWatcher.Configuration
{
    /// <summary>
    /// Contains information about all variables.
    /// </summary>
    [XmlRoot("variables")]
    public class Variables
    {
        /// <summary>
        /// Gets or sets the list of variables.
        /// </summary>
        [XmlElement("variable")]
        public Collection<Action>? VariableList { get; set; }
    }
}
