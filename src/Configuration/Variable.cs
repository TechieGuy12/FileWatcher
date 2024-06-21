using System.Xml.Serialization;

namespace TE.FileWatcher.Configuration
{
    public class Variable
    {
        [XmlElement("name")]
        public string Name { get; set; }

        [XmlElement("value")]
        public string Value { get; set; }
    }
}
