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
        /// <summary>
        /// The default log size.
        /// </summary>
        public const int DEFAULT_LOG_SIZE = 5;

        /// <summary>
        /// The default log number.
        /// </summary>
        public const int DEFAULT_LOG_NUMBER = 10;

        // The log path
        private string? _logPath;

        // The size of the log file
        private int _logSize = DEFAULT_LOG_SIZE;

        // The number of log files to retain
        private int _logNumber = DEFAULT_LOG_NUMBER;

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
        public int Size
        {
            get
            {
                return _logSize;
            }
            set
            {
                _logSize = value > 0 ? value : DEFAULT_LOG_SIZE;
            }
        }

        /// <summary>
        /// Gets or sets the number of log file to retain.
        /// </summary>
        [XmlElement("number")]
        public int Number
        { 
            get
            {
                return _logNumber;
            }
            set
            {
                _logNumber = value > 0 ? value : DEFAULT_LOG_NUMBER;
            }
        }

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
            Size = DEFAULT_LOG_SIZE;
            Number = DEFAULT_LOG_NUMBER;
        }
    }
}
