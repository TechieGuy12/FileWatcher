using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TE.FileWatcher.Configuration
{
    public abstract class ItemBase: HasVariablesBase
    {
        /// <summary>
        /// The object used to replace placeholders in strings.
        /// </summary>
        protected Placeholder Placeholder { get; } = new Placeholder();

        /// <summary>
        /// Gets or sets the change information.
        /// </summary>
        [XmlIgnore]
        public ChangeInfo? Change { get; set; }

        /// <summary>
        /// Gets or sets the triggers of the action.
        /// </summary>
        [XmlElement("triggers")]
        public Triggers Triggers { get; set; } = new Triggers();
    }
}
