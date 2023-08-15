using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TE.FileWatcher.Configuration
{
    public abstract class ItemBase
    {
        /// <summary>
        /// Gets or sets the change information.
        /// </summary>
        [XmlIgnore]
        protected static ChangeInfo? Change { get; set; }

        /// <summary>
        /// Gets or sets the triggers of the action.
        /// </summary>
        [XmlElement("triggers")]
        public Triggers Triggers { get; set; } = new Triggers();
    }
}
