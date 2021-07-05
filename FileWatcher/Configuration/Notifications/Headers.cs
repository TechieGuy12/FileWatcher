using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TE.FileWatcher.Configuration.Notifications
{
    public class Headers
    {
        [XmlElement("header")]
        public List<Header> HeaderList { get; set; }
    }
}
