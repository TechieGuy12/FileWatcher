using System.Collections.ObjectModel;
using System.Xml.Serialization;
using TE.FileWatcher.Log;

namespace TE.FileWatcher.Configuration
{
    /// <summary>
    /// The watches root node in the XML file.
    /// </summary>
    [XmlRoot("watches")]
    public class Watches
    {
        /// <summary>
        /// Gets or sets the logging information.
        /// </summary>
        [XmlElement("logging")]
        public Logging Logging { get; set; } = new Logging();

        /// <summary>
        /// Gets or sets the watches list.
        /// </summary>
        [XmlElement("watch")]
        public Collection<Watch>? WatchList { get; set; }

        /// <summary>
        /// Gets or sets the variables.
        /// </summary>
        [XmlElement("variables")]
        public Variables? Variables { get; set; }

        /// <summary>
        /// Starts the watches.
        /// </summary>
        public void Start()
        {
            if (WatchList == null || WatchList.Count <= 0)
            {
                Logger.WriteLine("No watches were specified.", LogLevel.ERROR);
                return;
            }
            Logger.WriteLine($"Log level: {Logger.LogLevel}.");
            foreach (Watch watch in WatchList)
            {
                try
                {
                    Task.Run(() => watch.Start(WatchList));
                }
                catch (Exception ex)
                {
                    Logger.WriteLine(ex.Message, LogLevel.ERROR);
                }
            }
        }
    }
}
