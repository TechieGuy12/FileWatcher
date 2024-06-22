using System.Collections.ObjectModel;
using System.Xml.Serialization;
using TE.FileWatcher.Log;

namespace TE.FileWatcher.Configuration
{
    /// <summary>
    /// The watches root node in the XML file.
    /// </summary>
    [XmlRoot("watches")]
    public class Watches : HasVariablesBase
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

            // Call the Add method with a null argument to add all the
            // variables for the watches element to a dictionary so it
            // can be passed to child elements
            Variables?.Add(null);
            foreach (Watch watch in WatchList)
            {
                try
                {
                    watch.Variables ??= new Variables();
                    Task.Run(() => watch.Start(WatchList, Variables?.AllVariables));
                }
                catch (Exception ex)
                {
                    Logger.WriteLine(ex.Message, LogLevel.ERROR);
                }
            }
        }
    }
}
