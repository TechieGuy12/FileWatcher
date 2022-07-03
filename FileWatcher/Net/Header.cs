using System.Xml.Serialization;

namespace TE.FileWatcher.Net
{
    public class Header
    {
        /// <summary>
        /// Gets or sets the name of the header.
        /// </summary>
        [XmlElement("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the value of othe header.
        /// </summary>
        [XmlElement("value")]
        public string? Value { get; set; }
    }
}
