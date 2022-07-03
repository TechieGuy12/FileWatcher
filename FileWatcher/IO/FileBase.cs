using System.Xml.Serialization;

namespace TE.FileWatcher.IO
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
#pragma warning disable CA2227
        public HashSet<Name> Name { get; set; } = new HashSet<Name>();
#pragma warning restore CA2227
    }
}
