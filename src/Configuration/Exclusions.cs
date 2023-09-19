using TE.FileWatcher.IO;

namespace TE.FileWatcher.Configuration
{
    /// <summary>
    /// An exclusions node in the XML file.
    /// </summary>
    public class Exclusions : MatchBase
    {
        /// <summary>
        /// Returns the flag indicating if the change is to be ignored.
        /// </summary>
        /// <param name="change">
        /// Information about the change.
        /// </param>
        /// <returns>
        /// True if the change is to be ignored, otherwise false.
        /// </returns>
        /// <exception cref="FileWatcherException">
        /// Thrown when there is a problem with the path.
        /// </exception>
        public bool Exclude(ChangeInfo change)
        {
            FilterTypeName = "Exclude";
            return IsMatchFound(change);
        }
    }
}
