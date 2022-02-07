using System.Text.RegularExpressions;
using System.Xml.Serialization;
using TE.FileWatcher.Logging;
using TEFS = TE.FileWatcher.FileSystem;

namespace TE.FileWatcher.Configuration.Actions
{
    /// <summary>
    /// The Action to perform during a watch event.
    /// </summary>
    public class Action : RunnableBase
    {
        // The regular expresson pattern for extracting the date type and the
        // specified date format to be used
        const string PATTERN = @"\[(?<datetype>.*):(?<format>.*)\]";

        // The created date placeholder value
        const string CREATED_DATE = "createddate";

        // The modified date placholder value
        const string MODIFIED_DATE = "modifieddate";

        // The current date placeholder value
        const string CURRENT_DATE = "currentdate";

        // The regular expression
        private readonly Regex _regex;

        /// <summary>
        /// The type of action to perform.
        /// </summary>
        [Serializable]
        public enum ActionType
        {
            /// <summary>
            /// Copy a file.
            /// </summary>
            Copy,
            /// <summary>
            /// Move a file.
            /// </summary>
            Move,
            /// <summary>
            /// Delete a file.
            /// </summary>
            Delete
        }

        /// <summary>
        /// Gets or sets the type of action to perform.
        /// </summary>
        [XmlElement("type")]
        public ActionType Type { get; set; }

        /// <summary>
        /// Gets or sets the source of the action.
        /// </summary>
        [XmlElement("source")]
        public string Source { get; set; } = PLACEHOLDER_FULLPATH;

        /// <summary>
        /// Gets or sets the triggers of the action.
        /// </summary>
        [XmlElement("triggers")]
        public Triggers Triggers { get; set; } = new Triggers();

        /// <summary>
        /// Gets or sets the destination of the action.
        /// </summary>
        [XmlElement("destination")]
        public string? Destination { get; set; }

        /// <summary>
        /// Gets or sets the verify flag.
        /// </summary>
        [XmlElement(ElementName = "verify", DataType = "boolean")]
        public bool Verify { get; set; }

        /// <summary>
        /// Creates an instance of the <see cref="Action"/> class.
        /// </summary>
        public Action()
        {
            _regex = new Regex(PATTERN, RegexOptions.Compiled);
        }

        /// <summary>
        /// Runs the action.
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
        public override void Run(string watchPath, string fullPath, TriggerType trigger)
        {
            if (string.IsNullOrWhiteSpace(watchPath) || string.IsNullOrWhiteSpace(fullPath))
            {
                return;
            }

            if (Triggers == null || Triggers.TriggerList == null)
            {
                return;
            }

            if (Triggers.TriggerList.Count <= 0 || !Triggers.Current.HasFlag(trigger))
            {
                return;
            }

            string? source = GetSource(watchPath, fullPath);
            string? destination = GetDestination(watchPath, fullPath);

            if (string.IsNullOrWhiteSpace(source))
            {
                Logger.WriteLine(
                    $"The source file could not be determined. Watch path: {watchPath}, changed: {fullPath}.",
                    LogLevel.ERROR);
                return;
            }

            if (!TEFS.File.IsValid(source))
            {
                Logger.WriteLine(
                    $"The file '{source}' could not be {GetActionString()} because the path was not valid, the file doesn't exists, or it was in use.",
                    LogLevel.ERROR);
                return;
            }

            try
            {
                switch (Type)
                {
                    case ActionType.Copy:
                        if (string.IsNullOrWhiteSpace(destination))
                        {
                            Logger.WriteLine(
                                $"The file '{source}' could not be copied because the destination file could not be determined. Destination in config file: {Destination}.",
                                LogLevel.ERROR);
                            return;
                        }

                        TEFS.File.Copy(source, destination, Verify);
                        Logger.WriteLine($"Copied {source} to {destination}.");
                        break;

                    case ActionType.Move:
                        if (string.IsNullOrWhiteSpace(destination))
                        {
                            Logger.WriteLine(
                                $"The file '{source}' could not be moved because the destination file could not be determined. Destination in config file: {Destination}.",
                                LogLevel.ERROR);
                            return;
                        }

                        TEFS.File.Move(source, destination, Verify);
                        Logger.WriteLine($"Moved {source} to {destination}.");
                        break;

                    case ActionType.Delete:
                        TEFS.File.Delete(source);
                        Logger.WriteLine($"Deleted {source}.");
                        break;
                }
            }
            catch (Exception ex)
            {
                string message = (ex.InnerException == null) ? ex.Message : ex.InnerException.Message;
                Logger.WriteLine(
                    $"Could not {Type.ToString().ToLower()} file '{source}.' Reason: {message}",
                    LogLevel.ERROR);
                return;
            }
        }

        /// <summary>
        /// Gets the string value that represents the action type.
        /// </summary>
        /// <returns>
        /// A string value for the action type, otherwise <c>null</c>.
        /// </returns>
        private string? GetActionString()
        {
            return Type switch
            {
                ActionType.Copy => "copied",
                ActionType.Move => "moved",
                ActionType.Delete => "deleted",
                _ => null
            };
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
                CREATED_DATE => TEFS.File.GetCreatedDate(fullPath),
                MODIFIED_DATE => TEFS.File.GetModifiedDate(fullPath),
                CURRENT_DATE => DateTime.Now,
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
        /// Gets the destination value by replacing any placeholders with the
        /// actual string values.
        /// </summary>
        /// <param name="watchPath">
        /// The watch path.
        /// </param>
        /// <param name="fullPath">
        /// The full path of the changed file.
        /// </param>
        /// <returns>
        /// The destination string value.
        /// </returns>
        private string? GetDestination(string watchPath, string fullPath)
        {
            if (string.IsNullOrWhiteSpace(Destination))
            {
                return null;
            }

            string? destination = ReplacePlaceholders(Destination, watchPath, fullPath);
            if (!string.IsNullOrWhiteSpace(destination))
            {
                destination = ReplaceDatePlaceholders(destination, watchPath, fullPath);
            }
            return destination;
        }

        /// <summary>
        /// Gets the source value by replacing any placeholders with the actual
        /// string values.
        /// </summary>
        /// <param name="watchPath">
        /// The watch path.
        /// </param>
        /// <param name="fullPath">
        /// The full path of the changed file.
        /// </param>
        /// <returns>
        /// The source string value.
        /// </returns>
        private string? GetSource(string watchPath, string fullPath)
        {
            if (string.IsNullOrWhiteSpace(Source))
            {
                return null;
            }

            return ReplacePlaceholders(Source, watchPath, fullPath);
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
        private string? ReplaceDatePlaceholders(string value, string watchPath, string fullPath)
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
                            } else
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
    }
}
