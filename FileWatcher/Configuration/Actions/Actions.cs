using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TE.FileWatcher.Logging;

namespace TE.FileWatcher.Configuration.Actions
{
    [XmlRoot("actions")]
    public class Actions
    {
        /// <summary>
        /// Gets or sets the list of actions to perform.
        /// </summary>
        [XmlElement("action")]
        public List<Action> ActionList { get; set; } = new List<Action>();
    }
}
