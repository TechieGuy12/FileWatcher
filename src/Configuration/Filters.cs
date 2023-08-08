using TE.FileWatcher.IO;

namespace TE.FileWatcher.Configuration
{
    /// <summary>
    /// A filters node in the XML file.
    /// </summary>
    public class Filters : MatchBase
    {
        /// <summary>
        /// Returns the flag indicating if the change is a match.
        /// </summary>
        /// <param name="change">
        /// Information about the change.
        /// </param>
        /// <returns>
        /// True if the change is to be ignored, otherwise false.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the <c>change</c> parameter is null.
        /// </exception>
        /// <exception cref="FileWatcherException">
        /// Thrown when there is a problem with the path.
        /// </exception>
        public bool IsMatch(ChangeInfo change)
        {
            if (change == null)
            {
                throw new ArgumentNullException(nameof(change));
            }

            FilterTypeName = "Filter";
            return IsMatchFound(change);
        }
    }
}
