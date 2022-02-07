using System.Xml.Serialization;

namespace TE.FileWatcher.Configuration.Notifications
{
    public class Header
    {
        [XmlElement("name")]
        public string? Name { get; set; }

        [XmlElement("value")]
        public string? Value { get; set; }
    }
}
