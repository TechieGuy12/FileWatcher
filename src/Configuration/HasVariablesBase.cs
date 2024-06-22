using System.Collections.Concurrent;
using System.Xml.Serialization;
using TE.FileWatcher.Log;

namespace TE.FileWatcher.Configuration
{
    public abstract class HasVariablesBase
    {
        /// <summary>
        /// Gets or sets the variables.
        /// </summary>
        [XmlElement("variables")]
        public Variables? Variables { get; set; }
    }
}
