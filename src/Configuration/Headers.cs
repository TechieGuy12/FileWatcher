using System;
using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace TE.FileWatcher.Configuration
{
    /// <summary>
    /// Contains the headers information.
    /// </summary>
    public class Headers : ItemBase
    {
        /// <summary>
        /// Get or sets the list of headers to add to a request.
        /// </summary>
        [XmlElement("header")]
        public Collection<Header>? HeaderList { get; set; }

        /// <summary>
        /// Sets the headers for a request.
        /// </summary>
        /// <param name="request">
        /// The request that will include the headers.
        /// </param>
        public void Set(HttpRequestMessage request)
        {
            if (request == null || Change == null)
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
                    string? value = header.Value;
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        value = Placeholder.ReplacePlaceholders(
                            value, 
                            Change.WatchPath,
                            Change.FullPath,
                            Change.OldPath);
                    }

                    request.Headers.Add(header.Name, value);
                }
            }
        }
    }
}
