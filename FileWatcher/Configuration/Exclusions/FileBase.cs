using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TE.FileWatcher.Configuration.Exclusions
{
    /// <summary>
    /// The base class used by the files and folders nodes in the XML file.
    /// </summary>
    public abstract class FileBase
    {
        /// <summary>
        /// Gets or sets the name of the file.
        /// </summary>
        [XmlElement("name")]
        public HashSet<string> Name { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}
