using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TE.FileWatcher.Notifications
{
    public class Data
    {
        [XmlElement("headers")]
        public Headers Headers { get; set; }

        [XmlElement("body")]
        public string Body { get; set; }
    }
}
