using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TE.FileWatcher.Configuration.Data;
using TE.FileWatcher.Logging;

namespace TE.FileWatcher.Configuration.Filters
{
    /// <summary>
    /// A filters node in the XML file.
    /// </summary>
    public class Filters : MatchBase
    {
        /// <summary>
        /// Returns the flag indicating if the change is a match.
        /// </summary>
        /// <param name="watchPath">
        /// The path associated with the watch.
        /// </param>
        /// <param name="name">
        /// The name of the file or folder.
        /// </param>
        /// <<param name="fullPath">
        /// The full path to the changed file or folder.
        /// </param>
        /// <returns>
        /// True if the change is to be ignored, otherwise false.
        /// </returns>
        /// <exception cref="FileWatcherException">
        /// Thrown when there is a problem with the path.
        /// </exception>
        public bool IsMatch(string watchPath, string name, string fullPath)
        {
            FilterTypeName = "Filter";
            return IsMatchFound(watchPath, name, fullPath);
        }
    }
}
