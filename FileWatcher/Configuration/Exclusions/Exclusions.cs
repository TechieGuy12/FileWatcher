using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TE.FileWatcher.Configuration.Exclusions
{
    /// <summary>
    /// An exclusions node in the XML file.
    /// </summary>
    public class Exclusions
    {
        /// <summary>
        /// Gets or sets the files node.
        /// </summary>
        [XmlElement("files")]
        public Files Files { get; set; } = new Files();

        /// <summary>
        /// Gets or sets the folders node.
        /// </summary>
        [XmlElement("folders")]
        public Folders Folders { get; set; } = new Folders();

        /// <summary>
        /// Gets or sets the paths node.
        /// </summary>
        [XmlElement("paths")]
        public Paths Paths { get; set; } = new Paths();

        /// <summary>
        /// Gets or sets the attributes node.
        /// </summary>
        [XmlElement("attributes")]
        public Attributes Attributes { get; set; } = new Attributes();
    }
}
