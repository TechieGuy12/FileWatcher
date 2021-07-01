using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TE.FileWatcher.Configuration
{
    /// <summary>
    /// Contains information about the log file.
    /// </summary>
    public class Logging
    {
        /// <summary>
        /// The default log file name.
        /// </summary>
        public const string DEFAULT_LOG_NAME = "fw.log";

        /// <summary>
        /// Gets or sets the path of the log file.
        /// </summary>
        [XmlElement("path")]
        public string LogPath { get; set; }

        /// <summary>
        /// Initializes an instance of the <see cref="Logging"/> class.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when the default log path could not be created.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the default log path could not be created.
        /// </exception>
        /// <exception cref="SecurityException">
        /// Thrown when the temporary folder can't be accessed by the user.
        /// </exception>
        public Logging() 
        {
            LogPath = Path.Combine(Path.GetTempPath(), DEFAULT_LOG_NAME);
        }
    }
}
