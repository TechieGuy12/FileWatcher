using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TE.FileWatcher.Configuration.Notifications
{
    /// <summary>
    /// Contains the data used to send the request.
    /// </summary>
    public class Data
    {
        // The MIME type
        private string _mimeType = Request.JSON_NAME;

        /// <summary>
        /// Gets or sets the headers for the request.
        /// </summary>
        [XmlElement("headers")]
        public Headers? Headers { get; set; }

        /// <summary>
        /// Gets or sets the body for the request.
        /// </summary>
        [XmlElement("body")]
        public string? Body { get; set; }

        /// <summary>
        /// Gets or sets the MIME type string value.
        /// </summary>
        [XmlElement("type")]
        public string MimeTypeString
        {
            get
            {
                return _mimeType;
            }
            set
            {
                _mimeType = (value == Request.JSON_NAME || value == Request.XML_NAME) ? value : Request.JSON_NAME;
            }
        }

        /// <summary>
        /// Gets the MIME type from the string value.
        /// </summary>
        [XmlIgnore]
        internal Request.MimeType MimeType
        {
            get
            {
                if (_mimeType == Request.XML_NAME)
                {
                    return Request.MimeType.Xml;
                }
                else
                {
                    return Request.MimeType.Json;
                }
            }
        }
    }
}
