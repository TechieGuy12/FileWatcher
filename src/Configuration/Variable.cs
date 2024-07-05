using System.Xml.Serialization;

namespace TE.FileWatcher.Configuration
{
    public class Variable
    {
        /// <summary>
        /// Gets or sets the name of the variable.
        /// </summary>
        [XmlElement("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the name of the variable.
        /// </summary>
        [XmlElement("value")]
        public string? Value { get; set; }
    }
}
