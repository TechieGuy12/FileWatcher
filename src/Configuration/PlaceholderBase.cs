using System.Text.RegularExpressions;
using System.IO;
using TE.FileWatcher.Log;
using TEFS = TE.FileWatcher.FileSystem;
using System.Xml.Serialization;
using System.Globalization;
using System.Web;
using System;

namespace TE.FileWatcher.Configuration
{
    public abstract class PlaceholderBase
    {
        // The exact path placeholder
        protected const string PLACEHOLDEREXACTPATH = "[exactpath]";

        // The full path placeholder
        protected const string PLACEHOLDERFULLPATH = "[fullpath]";

        // The path placholder
        protected const string PLACEHOLDERPATH = "[path]";

        // The file placeholder
        protected const string PLACEHOLDERFILE = "[file]";

        // The file name placeholder
        protected const string PLACEHOLDERFILENAME = "[filename]";

        // The file extension placeholder
        protected const string PLACEHOLDEREXTENSION = "[extension]";

        // The old exact path placeholder when a file/folder is renamed
        protected const string PLACEHOLDEROLDEXACTPATH = "[oldexactpath]";

        // The old full path placeholder when a file/folder is renamed
        protected const string PLACEHOLDEROLDFULLPATH = "[oldfullpath]";

        // The old path placholder when a file/folder is renamed
        protected const string PLACEHOLDEROLDPATH = "[oldpath]";

        // The old file placeholder when a file/folder is renamed
        protected const string PLACEHOLDEROLDFILE = "[oldfile]";

        // The old file name placeholder when a file/folder is renamed
        protected const string PLACEHOLDEROLDFILENAME = "[oldfilename]";

        // The old file extension placeholder when a file/folder is renamed
        protected const string PLACEHOLDEROLDEXTENSION = "[oldextension]";

        // The created date placeholder value
        protected const string PLACEHOLDERCREATEDDATE = "createddate";

        // The modified date placholder value
        protected const string PLACEHOLDERMODIFIEDDATE = "modifieddate";

        // The current date placeholder value
        protected const string PLACEHOLDERCURRENTDATE = "currentdate";

        // Environment variable placeholder value
        protected const string PLACEHOLDERENVVAR = "env";

        // The URL encode placeholder value
        protected const string PLACEHOLDERURLENCODE = "urlenc";

        // The regular expresson pattern for extracting the type and the
        // specified format/value to be used
        const string PATTERN = @"\[(?<type>.*?):(?<format>.*?)\]";

        // The regular expression
        private readonly Regex _regex;

        /// <summary>
        /// Gets or sets the change information.
        /// </summary>
        protected static ChangeInfo? Change { get; set; }

        /// <summary>
        /// Initializes an instance of the <see cref="PlaceholderBase"/> class.
        /// </summary>
        protected PlaceholderBase()
        {
            _regex = new Regex(PATTERN, RegexOptions.Compiled);
        }

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
        protected static string? ReplacePlaceholders(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || Change == null || string.IsNullOrWhiteSpace(Change.WatchPath))
            {
                return null;
            }

            string relativeFullPath = GetRelativeFullPath(Change.FullPath);
            string? relativePath = GetRelativePath(Change.FullPath);
            string? fileName = TEFS.File.GetName(Change.FullPath, true);
            string? fileNameWithoutExtension = TEFS.File.GetName(Change.FullPath, false);
            string? extension = TEFS.File.GetExtension(Change.FullPath);

            string replacedValue = value;
            replacedValue = replacedValue.Replace(PLACEHOLDEREXACTPATH, Change.FullPath, StringComparison.OrdinalIgnoreCase);
            replacedValue = replacedValue.Replace(PLACEHOLDERFULLPATH, relativeFullPath, StringComparison.OrdinalIgnoreCase);
            replacedValue = replacedValue.Replace(PLACEHOLDERPATH, relativePath, StringComparison.OrdinalIgnoreCase);
            replacedValue = replacedValue.Replace(PLACEHOLDERFILENAME, fileName, StringComparison.OrdinalIgnoreCase);
            replacedValue = replacedValue.Replace(PLACEHOLDERFILE, fileNameWithoutExtension, StringComparison.OrdinalIgnoreCase);
            replacedValue = replacedValue.Replace(PLACEHOLDEREXTENSION, extension, StringComparison.OrdinalIgnoreCase);

            // If the changes include an old path, such as when a file/folder
            // is renamed, then replace those placeholders
            if (!string.IsNullOrWhiteSpace(Change.OldPath))
            {
                string oldRelativeFullPath = GetRelativeFullPath(Change.OldPath);
                string? oldRelativePath = GetRelativePath(Change.OldPath);
                string? oldFileName = TEFS.File.GetName(Change.OldPath, true);
                string? oldFileNameWithoutExtension = TEFS.File.GetName(Change.OldPath, false);
                string? oldExtension = TEFS.File.GetExtension(Change.OldPath);

                replacedValue = replacedValue.Replace(PLACEHOLDEROLDEXACTPATH, Change.OldPath, StringComparison.OrdinalIgnoreCase);
                replacedValue = replacedValue.Replace(PLACEHOLDEROLDFULLPATH, relativeFullPath, StringComparison.OrdinalIgnoreCase);
                replacedValue = replacedValue.Replace(PLACEHOLDEROLDPATH, relativePath, StringComparison.OrdinalIgnoreCase);
                replacedValue = replacedValue.Replace(PLACEHOLDEROLDFILENAME, fileName, StringComparison.OrdinalIgnoreCase);
                replacedValue = replacedValue.Replace(PLACEHOLDEROLDFILE, fileNameWithoutExtension, StringComparison.OrdinalIgnoreCase);
                replacedValue = replacedValue.Replace(PLACEHOLDEROLDEXTENSION, extension, StringComparison.OrdinalIgnoreCase);
            }

            return replacedValue;
        }

        /// <summary>
        /// Replaces the formatted placeholders in a string with the actual values.
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
        protected string? ReplaceFormatPlaceholders(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (_regex.IsMatch(value))
            {
                // Find all the regex matches that are in the string since there
                // could be multiple date matches
                MatchCollection matches = _regex.Matches(value);
                if (matches.Count > 0)
                {
                    // Loop through each of the matches so the placeholder can
                    // be replaced with the actual date values
                    foreach (Match match in matches.Cast<Match>())
                    {

                        // Store the date type (createddate, modifieddate,
                        // or currentdate) and change it to lowercase so it can
                        // be easily compared later
                        string type = match.Groups["type"].Value.ToLower(CultureInfo.CurrentCulture);
                        // Store the specified date format
                        string format = match.Groups["format"].Value;
                        try
                        {
                            // Determine the type of date type, and then get
                            // the value for the date
                            switch (type)
                            {
                                case PLACEHOLDERCREATEDDATE:
                                case PLACEHOLDERMODIFIEDDATE:
                                case PLACEHOLDERCURRENTDATE:
                                    value = GetDateValue(match.Value, value, type, format);
                                    break;
                                case PLACEHOLDERENVVAR:
                                    value = GetEnvironmentVariableValue(match.Value, value, format);
                                    break;
                                case PLACEHOLDERURLENCODE:
                                    value = GetUrlEncodedValue(match.Value, value, format);
                                    break;
                            };

                        }
                        catch (FileWatcherException ex)
                        {
                            Logger.WriteLine(ex.Message, LogLevel.ERROR);
                            continue;
                        }
                    }
                }
            }

            return value;
        }

        /// <summary>
        /// Gets the date value for the specified date type and format, and
        /// replaces the date placeholder with the date value.
        /// </summary>
        /// <param name="placeholder">
        /// The placeholder in the value.
        /// </param>
        /// <param name="value">
        /// The string value containing the placeholder.
        /// </param>
        /// <param name="type">
        /// The date type.
        /// </param>
        /// <param name="format">
        /// The format of the date.
        /// </param>
        /// <returns>
        /// The date string value.
        /// </returns>
        private static string GetDateValue(string placeholder, string value, string type, string format)
        {
            if (Change == null)
            {
                Logger.WriteLine(
                    "The change information was not provided.",
                    LogLevel.WARNING);
                return value;
            }

            if (string.IsNullOrWhiteSpace(Change.FullPath))
            {
                Logger.WriteLine(
                    "The date could not be determined. The full path to the file was not provided.",
                    LogLevel.WARNING);
                return value;
            }

            // Get the date for the specified date type
            DateTime? date = GetDate(type);
            if (date != null)
            {
                // The string value for the date time using the date type
                // and format
                string? dateString = GetDateString((DateTime)date, format);

                // Replace the date placeholder with the formatted date
                // value
                value = value.Replace(placeholder, dateString, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                Logger.WriteLine(
                    $"The date value is null. Date type: {type}, changed: {Change.FullPath}, value: {value}, watch path: {Change.WatchPath}.",
                    LogLevel.WARNING);

            }

            return value;
        }

        /// <summary>
        /// Gets the environment variable value and replaces the environment
        /// variable placeholder with the environment variable value.
        /// </summary>
        /// <param name="placeholder">
        /// The placeholder in the value.
        /// </param>
        /// <param name="value">
        /// The string value containing the placeholder.
        /// </param>
        /// <param name="envName">
        /// The name of the environment variable.
        /// </param>
        /// <returns>
        /// The value of the environment variable.
        /// </returns>
        private static string GetEnvironmentVariableValue(string placeholder, string value, string envName)
        {
            if (Change == null)
            {
                Logger.WriteLine(
                    "The change information was not provided.",
                    LogLevel.WARNING);
                return value;
            }

            string? envValue = Environment.GetEnvironmentVariable(envName);
            if (envValue != null)
            {
                value = value.Replace(placeholder, envValue, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                Logger.WriteLine(
                $"The environment variable value is null. Environment variable: {envName}, changed: {Change.FullPath}, value: {value}, watch path: {Change.WatchPath}.",
                LogLevel.WARNING);
            }

            return value;
        }

        /// <summary>
        /// Encodes the URL value and replaces the URL encode placeholder with
        /// the URL encoded value.
        /// </summary>
        /// <param name="placeholder">
        /// The placeholder in the value.
        /// </param>
        /// <param name="value">
        /// The string value containing the placeholder.
        /// </param>
        /// <param name="url">
        /// The URL string.
        /// </param>
        /// <returns></returns>
        private static string GetUrlEncodedValue(string placeholder, string value, string url)
        {
            if (Change == null)
            {
                Logger.WriteLine(
                    "The change information was not provided.",
                    LogLevel.WARNING);
                return value;
            }

            string? encodedValue = HttpUtility.UrlEncode(url);
            if (encodedValue != null)
            {
                value = value.Replace(placeholder, encodedValue, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                Logger.WriteLine(
                    $"The URL value could not be encoded. URL: {url}, changed: {Change.FullPath}, value: {value}, watch path: {Change.WatchPath}.",
                    LogLevel.WARNING);
            }

            return value;
        }
        /// <summary>
        /// Gets the date value for the specified date type using the full path
        /// of the changed file.
        /// </summary>
        /// <param name="dateType">
        /// The type of date.
        /// </param>
        /// <returns>
        /// The <see cref="DateTime"/> value for the type, otherwise <c>null</c>.
        /// </returns>
        /// <exception cref="FileWatcherException">
        /// Thrown when the date could not be determined.
        /// </exception>
        private static DateTime? GetDate(string dateType)
        {
            if (Change == null)
            {
                Logger.WriteLine(
                    "The change information was not provided.",
                    LogLevel.WARNING);
                return null;
            }

            // Determine the type of date type, and then get
            // the value for the date
            return dateType switch
            {
                PLACEHOLDERCREATEDDATE => TEFS.File.GetCreatedDate(Change.FullPath),
                PLACEHOLDERMODIFIEDDATE => TEFS.File.GetModifiedDate(Change.FullPath),
                PLACEHOLDERCURRENTDATE => DateTime.Now,
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
                string? dateString = date.ToString(format, CultureInfo.CurrentCulture);
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
        /// <returns>
        /// The relative path.
        /// </returns>
        private static string GetRelativeFullPath(string fullPath)
        {
            if (Change == null)
            {
                Logger.WriteLine(
                    "The change information was not provided.",
                    LogLevel.WARNING);
                return "";
            }

            if (string.IsNullOrWhiteSpace(Change.WatchPath))
            {
                Logger.WriteLine(
                    "The watch path was not provided.",
                    LogLevel.WARNING);
                return "";
            }

            try
            {
                int index = fullPath.IndexOf(Change.WatchPath, StringComparison.OrdinalIgnoreCase);
                return (index < 0) ? fullPath : fullPath.Remove(index, Change.WatchPath.Length).Trim(Path.DirectorySeparatorChar);
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
        /// <param name="path">
        /// The path.
        /// </param>
        /// <returns>
        /// The relative path without the file name, otherwise <c>null</c>.
        /// </returns>
        private static string? GetRelativePath(string path)
        {
            if (Change == null)
            {
                Logger.WriteLine(
                    "The change information was not provided.",
                    LogLevel.WARNING);
                return "";
            }

            string? relativeFullPath = Path.GetDirectoryName(path);
            if (relativeFullPath == null)
            {
                return null;
            }

            return GetRelativeFullPath(relativeFullPath);
        }
    }
}
