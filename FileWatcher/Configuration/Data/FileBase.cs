using System.Xml.Serialization;

namespace TE.FileWatcher.Configuration.Data
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
        public HashSet<Name> Name { get; set; } = new HashSet<Name>();
    }
}
