using System.Text.RegularExpressions;
using IO = System.IO;
using TE.FileWatcher.Logging;
using TEFS = TE.FileWatcher.FileSystem;
using System.Xml.Serialization;

namespace TE.FileWatcher.Configuration
{
    /// <summary>
    /// A base abstract class for the classes which require execution on the
    /// machine and includes placeholders in the data that need to be replaced.
    /// </summary>
    public abstract class RunnableBase
    {
        // The exact path placeholder
        protected const string PLACEHOLDER_EXACTPATH = "[exactpath]";

        // The full path placeholder
        protected const string PLACEHOLDER_FULLPATH = "[fullpath]";

        // The path placholder
        protected const string PLACEHOLDER_PATH = "[path]";

        // The file placeholder
        protected const string PLACEHOLDER_FILE = "[file]";

        // The file name placeholder
        protected const string PLACEHOLDER_FILENAME = "[filename]";

        // The file extension placeholder
        protected const string PLACEHOLDER_EXTENSION = "[extension]";

        // The created date placeholder value
        protected const string PLACEHOLDER_CREATED_DATE = "createddate";

        // The modified date placholder value
        protected const string PLACEHOLDER_MODIFIED_DATE = "modifieddate";

        // The current date placeholder value
        protected const string PLACEHOLDER_CURRENT_DATE = "currentdate";

        // The regular expresson pattern for extracting the date type and the
        // specified date format to be used
        const string PATTERN = @"\[(?<datetype>.*):(?<format>.*)\]";

        // The regular expression
        private readonly Regex _regex;

        /// <summary>
        /// Gets or sets the number of milliseconds to wait before running.
        /// </summary>
        [XmlElement("waitbefore")]
        public int WaitBefore { get; set; }

        /// <summary>
        /// Initializes an instance of the <see cref="RunnableBase"/> class.
        /// </summary>
        public RunnableBase()
        {
            _regex = new Regex(PATTERN, RegexOptions.Compiled);
        }

        /// <summary>
        /// The abstract method to Run.
        /// </summary>
        /// <param name="watchPath">
        /// The watch path.
        /// </param>
        /// <param name="fullPath">
        /// The full path to the changed file or folder.
        /// </param>
        /// <param name="trigger">
        /// The trigger for the action.
        /// </param>
        public abstract void Run(string watchPath, string fullPath, TriggerType trigger);

        /// <summary>
        /// Replaces the placeholders in a string with the actual values.
        /// </summary>
        /// <param name="value">
        /// The value containing the placeholders.
        /// </param>
        /// <param name="watchPath">
        /// The watch path.
        /// </param>
        /// <param name="fullPath">
        /// The full path of the changed file.
        /// </param>
        /// <returns>
        /// The value with the placeholders replaced with the actual strings.
        /// </returns>
        protected static string? ReplacePlaceholders(string value, string watchPath, string fullPath)
        {
            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(watchPath) || string.IsNullOrWhiteSpace(fullPath))
            {
                return null;
            }

            string relativeFullPath = GetRelativeFullPath(watchPath, fullPath);
            string? relativePath = GetRelativePath(watchPath, fullPath);
            string? fileName = TEFS.File.GetName(fullPath, true);
            string? fileNameWithoutExtension = TEFS.File.GetName(fullPath, false);
            string? extension = TEFS.File.GetExtension(fullPath);

            string replacedValue = value;
            replacedValue = replacedValue.Replace(PLACEHOLDER_EXACTPATH, fullPath);
            replacedValue = replacedValue.Replace(PLACEHOLDER_FULLPATH, relativeFullPath);
            replacedValue = replacedValue.Replace(PLACEHOLDER_PATH, relativePath);
            replacedValue = replacedValue.Replace(PLACEHOLDER_FILENAME, fileName);
            replacedValue = replacedValue.Replace(PLACEHOLDER_FILE, fileNameWithoutExtension);
            replacedValue = replacedValue.Replace(PLACEHOLDER_EXTENSION, extension);

            return replacedValue;
        }

        /// <summary>
        /// Replaces the date placeholders in a string with the actual values.
        /// </summary>
        /// <param name="value">
        /// The value containing the placeholders.
        /// </param>
        /// <param name="watchPath">
        /// The watch path.
        /// </param>
        /// <param name="fullPath">
        /// The full path of the changed file.
        /// </param>
        /// <returns>
        /// The value with the placeholders replaced with the actual strings,
        /// otherwise <c>null</c>.
        /// </returns>
        protected string? ReplaceDatePlaceholders(string value, string watchPath, string fullPath)
        {
            // Re
            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(watchPath) || string.IsNullOrWhiteSpace(fullPath))
            {
                return null;
            }

            string replacedValue = value;

            if (_regex.IsMatch(value))
            {
                // Find all the regex matches that are in the string since there
                // could be multiple date matches
                MatchCollection matches = _regex.Matches(value);
                if (matches.Count > 0)
                {
                    // Loop through each of the matches so the placeholder can
                    // be replaced with the actual date values
                    foreach (Match match in matches)
                    {
                        // Store the date type (createddate, modifieddate,
                        // or currentdate) and change it to lowercase so it can
                        // be easily compared later
                        string dateType = match.Groups["datetype"].Value.ToLower();
                        // Store the specified date format
                        string format = match.Groups["format"].Value;

                        try
                        {
                            // Get the date for the specified date type
                            DateTime? date = GetDate(dateType, fullPath);
                            if (date != null)
                            {
                                // The string value for the date time using the date type
                                // and format
                                string? dateString = GetDateString((DateTime)date, format);

                                // Replace the date placeholder with the formatted date
                                // value
                                replacedValue = replacedValue.Replace(match.Value, dateString);
                            }
                            else
                            {
                                Logger.WriteLine(
                                    $"The date value is null. Date type: {dateType}, changed: {fullPath}, value: {value}, watch path: {watchPath}.",
                                    LogLevel.WARNING);
                            }
                        }
                        catch (FileWatcherException ex)
                        {
                            Logger.WriteLine(ex.Message, LogLevel.ERROR);
                            continue;
                        }
                    }
                }
            }

            return replacedValue;
        }

        /// <summary>
        /// Gets the date value for the specified date type using the full path
        /// of the changed file.
        /// </summary>
        /// <param name="dateType">
        /// The type of date.
        /// </param>
        /// <param name="fullPath">
        /// The full path to the changed file.
        /// </param>
        /// <returns>
        /// The <see cref="DateTime"/> value for the type, otherwise <c>null</c>.
        /// </returns>
        /// <exception cref="FileWatcherException">
        /// Thrown when the date could not be determined.
        /// </exception>
        private static DateTime? GetDate(string dateType, string fullPath)
        {
            // Determine the type of date type, and then get
            // the value for the date
            return dateType switch
            {
                PLACEHOLDER_CREATED_DATE => TEFS.File.GetCreatedDate(fullPath),
                PLACEHOLDER_MODIFIED_DATE => TEFS.File.GetModifiedDate(fullPath),
                PLACEHOLDER_CURRENT_DATE => DateTime.Now,
                _ => null
            };
        }

        /// <summary>
        /// Gets the date string value using the specified date and format.
        /// </summary>
        /// <param name="date">
        /// The date to be formatted.
        /// </param>
        /// <param name="format">
        /// The format string.
        /// </param>
        /// <returns>
        /// The formatted string value
        /// </returns>
        /// <exception cref="FileWatcherException">
        /// Thrown when the date string value can not be created.
        /// </exception>
        private static string? GetDateString(DateTime date, string format)
        {
            if (string.IsNullOrEmpty(format))
            {
                Logger.WriteLine("The date format was not provided.");
                return null;
            }

            try
            {
                // Format the date, or return null if there is an
                // issue trying to format the date
                string? dateString = date.ToString(format);
                if (string.IsNullOrWhiteSpace(dateString))
                {
                    // There was an issue formatting the date, and
                    // the date string value was null or contained
                    // no value, so write a log message, and then
                    // continue to the next match
                    throw new FileWatcherException(
                        $"The date could not be formatted. Format: {format}, date: {date}.");
                }

                return dateString;
            }
            catch (Exception ex)
                when (ex is ArgumentException || ex is FormatException)
            {
                throw new FileWatcherException(
                    $"The date could not be formatted properly using '{format}'. Reason: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the relative path from the watch path using the full path.
        /// </summary>
        /// <param name="watchPath">
        /// The watch path.
        /// </param>
        /// <param name="fullPath">
        /// The full path.
        /// </param>
        /// <returns>
        /// The relative path.
        /// </returns>
        private static string GetRelativeFullPath(string watchPath, string fullPath)
        {
            if (string.IsNullOrWhiteSpace(watchPath) || string.IsNullOrWhiteSpace(fullPath))
            {
                return fullPath;
            }

            try
            {
                int index = fullPath.IndexOf(watchPath, StringComparison.OrdinalIgnoreCase);
                return (index < 0) ? fullPath : fullPath.Remove(index, watchPath.Length).Trim(IO.Path.DirectorySeparatorChar);
            }
            catch (Exception ex)
                when (ex is ArgumentException || ex is ArgumentNullException)
            {
                return fullPath;
            }
        }

        /// <summary>
        /// Gets the relative path without the file name from the watch path
        /// using the full path.
        /// </summary>
        /// <param name="watchPath">
        /// The watch path.
        /// </param>
        /// <param name="fullPath">
        /// The full path.
        /// </param>
        /// <returns>
        /// The relative path without the file name, otherwise <c>null</c>.
        /// </returns>
        private static string? GetRelativePath(string watchPath, string fullPath)
        {
            string? relativeFullPath = Path.GetDirectoryName(fullPath);
            if (relativeFullPath == null)
            {
                return null;
            }

            return GetRelativeFullPath(watchPath, relativeFullPath);
        }
    }
}
