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
                return;
            }

            try
            {
                switch (Type)
                {
                    case ActionType.Copy:
                        if (TEFS.File.IsValid(source))
                        {
                            if (string.IsNullOrWhiteSpace(destination))
                            {
                                Logger.WriteLine(
                                    $"The file '{source}' could not be copied because the destination file was not specified.",
                                    LogLevel.ERROR);
                                return;
                            }

                            TEFS.File.Copy(source, destination, Verify);
                            Logger.WriteLine($"Copied {source} to {destination}.");
                        }
                        else
                        {
                            Logger.WriteLine(
                                $"The file '{source}' could not be copied because the path was not valid, the file doesn't exists, or it was in use.",
                                LogLevel.ERROR);
                        }
                        break;
                    case ActionType.Move:
                        if (TEFS.File.IsValid(source))
                        {
                            if (string.IsNullOrWhiteSpace(destination))
                            {
                                Logger.WriteLine($"The file '{source}' could not be moved because the destination file was not specified.");
                                return;
                            }

                            TEFS.File.Move(source, destination, Verify);
                            Logger.WriteLine($"Moved {source} to {destination}.");
                        }
                        else
                        {
                            Logger.WriteLine(
                                $"The file '{source}' could not be moved because the path was not valid, the file doesn't exists, or it was in use.",
                                LogLevel.ERROR);
                        }
                        break;
                    case ActionType.Delete:
                        if (TEFS.File.IsValid(source))
                        {
                            TEFS.File.Delete(source);
                            Logger.WriteLine($"Deleted {source}.");
                        }
                        else
                        {
                            Logger.WriteLine(
                                $"The file '{source}' could not be deleted because the path was not valid, the file doesn't exists, or it was in use.", 
                                LogLevel.ERROR);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                string message = (ex.InnerException == null) ? ex.Message : ex.InnerException.Message;
                Logger.WriteLine($"Could not {Type.ToString().ToLower()} file {source}. Reason: {message}", LogLevel.ERROR);
                return;
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

                        DateTime? date;
                        try
                        {
                            // Determine the type of date type, and then get
                            // the value for the date
                            switch (dateType)
                            {
                                case CREATED_DATE:
                                    date = TEFS.File.GetCreatedDate(fullPath);
                                    break;
                                case MODIFIED_DATE:
                                    date = TEFS.File.GetModifiedDate(fullPath);
                                    break;
                                case CURRENT_DATE:
                                    date = DateTime.Now;
                                    break;
                                default:
                                    date = null;
                                    break;
                            }
                        }
                        catch (FileWatcherException ex)
                        {
                            date = null;
                            Logger.WriteLine(ex.Message, LogLevel.ERROR);
                        }

                        string? dateString;
                        try
                        {
                            // Format the date, or return null if there is an
                            // issue trying to format the date
                            dateString = date?.ToString(format);
                            if (string.IsNullOrWhiteSpace(dateString))
                            {
                                // There was an issue formatting the date, and
                                // the date string value was null or contained
                                // no value, so write a log message, and then
                                // continue to the next match
                                Logger.WriteLine(
                                    $"The date could not be formatted. Format: {format}, date: {date?.ToString()}, destination: {value}, source: {fullPath}",
                                    LogLevel.WARNING);
                                continue;
                            }
                        }
                        catch (Exception ex)
                            when (ex is ArgumentException || ex is FormatException)
                        {
                            Logger.WriteLine(
                                $"The date could not be formatted properly using '{format}'. Reason: {ex.Message}",
                                LogLevel.ERROR);
                            continue;
                        }

                        // Replace the date placeholder with the formatted date
                        // value
                        replacedValue = replacedValue.Replace(match.Value, dateString);
                    }
                }
            }    

            return replacedValue;
        }
    }
}
