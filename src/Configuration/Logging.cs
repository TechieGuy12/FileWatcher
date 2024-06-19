using System.Security;
using System.Xml.Serialization;
using TE.FileWatcher.Log;

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
        public const int DEFAULTLOGSIZE = 5;

        /// <summary>
        /// The default log number.
        /// </summary>
        public const int DEFAULTLOGNUMBER = 10;

        // The log path
        private string? _logPath;

        // The size of the log file
        private int _logSize = DEFAULTLOGSIZE;

        // The number of log files to retain
        private int _logNumber = DEFAULTLOGNUMBER;

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
                _logPath = !string.IsNullOrWhiteSpace(value) ?  value : Path.Combine(Path.GetTempPath(), Logger.DEFAULTLOGNAME);
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
                _logSize = value > 0 ? value : DEFAULTLOGSIZE;
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
                _logNumber = value > 0 ? value : DEFAULTLOGNUMBER;
            }
        }

        /// <summary>
        /// Gets or sets the log levl.
        /// </summary>
        [XmlElement("level")]
        public LogLevel Level { get; set; } = LogLevel.INFO;

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
            LogPath = Path.Combine(Path.GetTempPath(), Logger.DEFAULTLOGNAME);
            Size = DEFAULTLOGSIZE;
            Number = DEFAULTLOGNUMBER;
            Level = LogLevel.INFO;
        }
    }
}
