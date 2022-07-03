using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace TE.FileWatcher.Net
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
#pragma warning disable CA2227 // Collection properties should be read only
        public Collection<Header>? HeaderList { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

        /// <summary>
        /// Sets the headers for a request.
        /// </summary>
        /// <param name="request">
        /// The request that will include the headers.
        /// </param>
        public void Set(HttpRequestMessage request)
        {
            if (request == null)
            {
                return;
            }

            if (HeaderList == null || HeaderList.Count <= 0)
            {
                return;
            }

            foreach (Header header in HeaderList)
            {
                if (!string.IsNullOrWhiteSpace(header.Name))
                {
                    request.Headers.Add(header.Name, header.Value);
                }
            }
        }
    }
}
