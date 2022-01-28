using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TE.FileWatcher.Logging;

namespace TE.FileWatcher.Configuration
{
    /// <summary>
    /// Contains information about the log file.
    /// </summary>
    public class Logging
    {
        // The log path
        private string? _logPath;

        /// <summary>
        /// Gets or sets the path of the log file.
        /// </summary>
        [XmlElement("path")]
        public string? LogPath
        { 
            get
            {
                return _logPath;
            }
            set
            {
                _logPath = !string.IsNullOrWhiteSpace(value) ?  value : Path.Combine(Path.GetTempPath(), Logger.DEFAULT_LOG_NAME);
            }
        }

        /// <summary>
        /// Gets or sets the size (in megabytes) of a log file before it is
        /// backed up and a new log file is created.
        /// </summary>
        [XmlElement("size")]
        public int Size { get; set; }

        /// <summary>
        /// Gets or sets the number of log file to retain.
        /// </summary>
        [XmlElement("number")]
        public int Number { get; set; }

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
            LogPath = Path.Combine(Path.GetTempPath(), Logger.DEFAULT_LOG_NAME);
            Size = 5;
            Number = 10;
        }
    }
}
