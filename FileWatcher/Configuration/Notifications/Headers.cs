using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Net.Http;

namespace TE.FileWatcher.Configuration.Notifications
{
    /// <summary>
    /// Contains the headers information.
    /// </summary>
    public class Headers
    {
        /// <summary>
        /// Get or sets the list of headers to add to a request.
        /// </summary>
        [XmlElement("header")]
        public List<Header> HeaderList { get; set; }

        /// <summary>
        /// Sets the headers for a request.
        /// </summary>
        /// <param name="request">
        /// The request that will include the headers.
        /// </param>
        public void Set(HttpRequestMessage request)
        {
            if (HeaderList.Count <= 0)
            {
                return;
            }

            foreach (Header header in HeaderList)
            {
                request.Headers.Add(header.Name, header.Value);
            }
        }
    }
}
