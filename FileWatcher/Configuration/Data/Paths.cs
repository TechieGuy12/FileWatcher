using System.Xml.Serialization;

namespace TE.FileWatcher.Configuration.Data
{
    /// <summary>
    /// The paths node within the exclusions node in the XML file.
    /// </summary>
    public class Paths
    {
        /// <summary>
        /// Gets or sets a list of paths.
        /// </summary>
        [XmlElement("path")]
        public HashSet<string> Path { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}
