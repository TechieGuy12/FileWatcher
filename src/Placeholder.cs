using System.Text.RegularExpressions;
using TE.FileWatcher.Log;
using TEFS = TE.FileWatcher.FileSystem;
using System.Globalization;
using System.Web;
using System;
using System.Collections.Concurrent;

namespace TE.FileWatcher
{
    public sealed class Placeholder
    {
        // The watch path placeholder
        internal const string PLACEHOLDERWATCHPATH = "[watchpath]";

        // The exact path placeholder
        internal const string PLACEHOLDEREXACTPATH = "[exactpath]";

        // The full path placeholder
        internal const string PLACEHOLDERFULLPATH = "[fullpath]";

        // The path placholder
        internal const string PLACEHOLDERPATH = "[path]";

        // The file placeholder
        internal const string PLACEHOLDERFILE = "[file]";

        // The file name placeholder
        internal const string PLACEHOLDERFILENAME = "[filename]";

        // The file extension placeholder
        internal const string PLACEHOLDEREXTENSION = "[extension]";

        // The old exact path placeholder when a file/folder is renamed
        internal const string PLACEHOLDEROLDEXACTPATH = "[oldexactpath]";

        // The old full path placeholder when a file/folder is renamed
        internal const string PLACEHOLDEROLDFULLPATH = "[oldfullpath]";

        // The old path placholder when a file/folder is renamed
        internal const string PLACEHOLDEROLDPATH = "[oldpath]";

        // The old file placeholder when a file/folder is renamed
        internal const string PLACEHOLDEROLDFILE = "[oldfile]";

        // The old file name placeholder when a file/folder is renamed
        internal const string PLACEHOLDEROLDFILENAME = "[oldfilename]";

        // The old file extension placeholder when a file/folder is renamed
        internal const string PLACEHOLDEROLDEXTENSION = "[oldextension]";

        // The created date placeholder value
        internal const string PLACEHOLDERCREATEDDATE = "createddate";

        // The modified date placholder value
        internal const string PLACEHOLDERMODIFIEDDATE = "modifieddate";

        // The current date placeholder value
        internal const string PLACEHOLDERCURRENTDATE = "currentdate";

        // Environment variable placeholder value
        internal const string PLACEHOLDERENVVAR = "env";

        // Variable placeholder value
        internal const string PLACEHOLDERVAR = "var";

        // The URL encode placeholder value
        internal const string PLACEHOLDERURLENCODE = "urlenc";

        // The regular expresson pattern for extracting the type and the
        // specified format/value to be used
        const string PATTERN = @"\[(?<type>.*?):(?<format>.*?)\]";

        // The regular expression
        private static readonly Regex _regex = new Regex(PATTERN, RegexOptions.Compiled);

        // Cache for static placeholder replacements (env vars, variables, URL encoding)
        // Key: original placeholder pattern, Value: resolved value
        private static readonly ConcurrentDictionary<string, string> _placeholderCache = 
            new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Maximum cache size before clearing to prevent unbounded memory growth
        private const int MAX_CACHE_SIZE = 1000;

        // Cache statistics for monitoring
        private static long _cacheHits = 0;
        private static long _cacheMisses = 0;

        private ConcurrentDictionary<string, string>? _variables;

        /// <summary>
        /// Gets the cache hit rate for monitoring performance optimization effectiveness.
        /// </summary>
        internal static double CacheHitRate
        {
            get
            {
                long hits = Interlocked.Read(ref _cacheHits);
                long misses = Interlocked.Read(ref _cacheMisses);
                long total = hits + misses;
                return total == 0 ? 0 : (double)hits / total;
            }
        }

        /// <summary>
        /// Clears the placeholder cache. Useful for testing or when cache needs to be refreshed.
        /// </summary>
        internal static void ClearCache()
        {
            _placeholderCache.Clear();
            Interlocked.Exchange(ref _cacheHits, 0);
            Interlocked.Exchange(ref _cacheMisses, 0);
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
        /// <param name="oldPath">
        /// The old path to the file or folder.
        /// </param>
        /// <returns>
        /// The value with the placeholders replaced with the actual strings,
        /// otherwise <c>null</c>.
        /// </returns>
        internal string? ReplacePlaceholders(string value, string watchPath, string fullPath, string? oldPath, ConcurrentDictionary<string, string>? variables)
        {
            _variables = variables;
            string? changedValue = ReplaceFileFolderPlaceholders(value, watchPath, fullPath, oldPath);
            if (!string.IsNullOrWhiteSpace(changedValue))
            {
                changedValue = ReplaceFormatPlaceholders(changedValue, fullPath);
            }            

            return changedValue;
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
        private DateTime? GetDate(string dateType, string fullPath)
        {

            // Determine the type of date type, and then get
            // the value for the date
            return dateType switch
            {
                PLACEHOLDERCREATEDDATE => TEFS.File.GetCreatedDate(fullPath),
                PLACEHOLDERMODIFIEDDATE => TEFS.File.GetModifiedDate(fullPath),
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
        private string? GetDateString(DateTime date, string format)
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
        private string GetDateValue(string placeholder, string value, string type, string format, string fullPath)
        {
            // Get the date for the specified date type
            DateTime? date = GetDate(type, fullPath);
            if (date != null)
            {
                // The string value for the date time using the date type
                // and format
                string? dateString = GetDateString((DateTime)date, format);

                // Replace the date placeholder with the formatted date
                // value
                value = value.Replace(placeholder, dateString, StringComparison.OrdinalIgnoreCase);
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
        private string GetEnvironmentVariableValue(string placeholder, string value, string envName)
        {

            string? envValue = Environment.GetEnvironmentVariable(envName);
            if (envValue != null)
            {
                value = value.Replace(placeholder, envValue, StringComparison.OrdinalIgnoreCase);
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
        /// <param name="name">
        /// The name of the variable.
        /// </param>
        /// <returns>
        /// The value of the variable.
        /// </returns>
        private string GetVariableValue(string placeholder, string value, string name)
        {
            if (_variables == null)
            {
                return value;
            }

            if (_variables.ContainsKey(name))
            {
                return value.Replace(placeholder, _variables[name], StringComparison.OrdinalIgnoreCase);
            }

            return value;
        }

        /// <summary>
        /// Gets the relative path from the watch path using the full path.
        /// </summary>
        /// <returns>
        /// The relative path.
        /// </returns>
        private string GetRelativeFullPath(string fullPath, string watchPath)
        {
            if (string.IsNullOrWhiteSpace(watchPath))
            {
                return "";
            }

            try
            {
                int index = fullPath.IndexOf(watchPath, StringComparison.OrdinalIgnoreCase);
                return index < 0 ? fullPath : fullPath.Remove(index, watchPath.Length).Trim(Path.DirectorySeparatorChar);
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
        private string? GetRelativePath(string path, string watchPath)
        {
            string? relativeFullPath = Path.GetDirectoryName(path);
            if (relativeFullPath == null)
            {
                return null;
            }

            return GetRelativeFullPath(relativeFullPath, watchPath);
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
        private string GetUrlEncodedValue(string placeholder, string value, string url)
        {
            string? encodedValue = HttpUtility.UrlEncode(url);
            if (encodedValue != null)
            {
                value = value.Replace(placeholder, encodedValue, StringComparison.OrdinalIgnoreCase);
            }

            return value;
        }

        /// <summary>
        /// Gets the value with the folder placeholders replaced with the
        /// correct file and folder information.
        /// </summary>
        /// <param name="value">
        /// The value containing the placeholders.
        /// </param>
        /// <param name="watchPath">
        /// The watch path.
        /// </param>
        /// <param name="fullPath">
        /// The full path to the file or folder.
        /// </param>
        /// <param name="oldPath">
        /// The old path to the file or folder.
        /// </param>
        /// <returns>
        /// The value with the placeholders replaced with the actual strings,
        /// otherwise <c>null</c>.
        /// </returns>
        private string? ReplaceFileFolderPlaceholders(string value, string watchPath, string fullPath, string? oldPath)
        {
            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(fullPath))
            {
                return null;
            }

            // Pre-compute all replacement values
            string relativeFullPath = GetRelativeFullPath(fullPath, watchPath);
            string? relativePath = GetRelativePath(fullPath, watchPath);
            string? fileName = TEFS.File.GetName(fullPath, true);
            string? fileNameWithoutExtension = TEFS.File.GetName(fullPath, false);
            string? extension = TEFS.File.GetExtension(fullPath);

            // Build replacement dictionary to avoid chained string allocations
            var replacements = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                [PLACEHOLDERWATCHPATH] = watchPath,
                [PLACEHOLDEREXACTPATH] = fullPath,
                [PLACEHOLDERFULLPATH] = relativeFullPath,
                [PLACEHOLDERPATH] = relativePath,
                [PLACEHOLDERFILENAME] = fileName,
                [PLACEHOLDERFILE] = fileNameWithoutExtension,
                [PLACEHOLDEREXTENSION] = extension
            };

            // If the changes include an old path, add those replacements
            if (!string.IsNullOrWhiteSpace(oldPath))
            {
                string oldRelativeFullPath = GetRelativeFullPath(oldPath, watchPath);
                string? oldRelativePath = GetRelativePath(oldPath, watchPath);
                string? oldFileName = TEFS.File.GetName(oldPath, true);
                string? oldFileNameWithoutExtension = TEFS.File.GetName(oldPath, false);
                string? oldExtension = TEFS.File.GetExtension(oldPath);

                replacements[PLACEHOLDEROLDEXACTPATH] = oldPath;
                replacements[PLACEHOLDEROLDFULLPATH] = oldRelativeFullPath;
                replacements[PLACEHOLDEROLDPATH] = oldRelativePath;
                replacements[PLACEHOLDEROLDFILENAME] = oldFileName;
                replacements[PLACEHOLDEROLDFILE] = oldFileNameWithoutExtension;
                replacements[PLACEHOLDEROLDEXTENSION] = oldExtension;
            }

            // Perform all replacements efficiently
            string replacedValue = value;
            foreach (var kvp in replacements)
            {
                if (kvp.Value != null)
                {
                    replacedValue = replacedValue.Replace(kvp.Key, kvp.Value, StringComparison.OrdinalIgnoreCase);
                }
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
        private string? ReplaceFormatPlaceholders(string value, string fullPath)
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
                    // Use indexed access instead of Cast<Match>() to avoid LINQ allocation
                    for (int i = 0; i < matches.Count; i++)
                    {
                        Match match = matches[i];

                        // Store the date type and convert to lowercase once
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
                                    // Dynamic date values cannot be cached
                                    value = GetDateValue(match.Value, value, type, format, fullPath);
                                    break;
                                case PLACEHOLDERENVVAR:
                                    value = GetCachedEnvironmentVariableValue(match.Value, value, format);
                                    break;
                                case PLACEHOLDERVAR:
                                    value = GetCachedVariableValue(match.Value, value, format);
                                    break;
                                case PLACEHOLDERURLENCODE:
                                    value = GetCachedUrlEncodedValue(match.Value, value, format);
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
        /// Gets the environment variable value with caching and replaces the environment
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
        private string GetCachedEnvironmentVariableValue(string placeholder, string value, string envName)
        {
            // Try to get from cache first
            if (_placeholderCache.TryGetValue(placeholder, out string? cachedValue))
            {
                Interlocked.Increment(ref _cacheHits);
                return value.Replace(placeholder, cachedValue, StringComparison.OrdinalIgnoreCase);
            }

            Interlocked.Increment(ref _cacheMisses);
            
            string? envValue = Environment.GetEnvironmentVariable(envName);
            if (envValue != null)
            {
                // Check cache size and clear if needed
                if (_placeholderCache.Count >= MAX_CACHE_SIZE)
                {
                    Logger.WriteLine(
                        $"Placeholder cache size limit reached ({MAX_CACHE_SIZE}), clearing cache.",
                        LogLevel.DEBUG);
                    _placeholderCache.Clear();
                }

                // Cache the resolved value
                _placeholderCache.TryAdd(placeholder, envValue);
                return value.Replace(placeholder, envValue, StringComparison.OrdinalIgnoreCase);
            }

            return value;
        }

        /// <summary>
        /// Gets the variable value with caching and replaces the variable
        /// placeholder with the variable value.
        /// </summary>
        /// <param name="placeholder">
        /// The placeholder in the value.
        /// </param>
        /// <param name="value">
        /// The string value containing the placeholder.
        /// </param>
        /// <param name="name">
        /// The name of the variable.
        /// </param>
        /// <returns>
        /// The value of the variable.
        /// </returns>
        private string GetCachedVariableValue(string placeholder, string value, string name)
        {
            if (_variables == null)
            {
                return value;
            }

            // Create cache key that includes variable name for uniqueness
            string cacheKey = $"{placeholder}:{name}";

            // Try to get from cache first
            if (_placeholderCache.TryGetValue(cacheKey, out string? cachedValue))
            {
                Interlocked.Increment(ref _cacheHits);
                return value.Replace(placeholder, cachedValue, StringComparison.OrdinalIgnoreCase);
            }

            Interlocked.Increment(ref _cacheMisses);

            if (_variables.ContainsKey(name))
            {
                string varValue = _variables[name];
                
                // Check cache size and clear if needed
                if (_placeholderCache.Count >= MAX_CACHE_SIZE)
                {
                    Logger.WriteLine(
                        $"Placeholder cache size limit reached ({MAX_CACHE_SIZE}), clearing cache.",
                        LogLevel.DEBUG);
                    _placeholderCache.Clear();
                }

                // Cache the resolved value
                _placeholderCache.TryAdd(cacheKey, varValue);
                return value.Replace(placeholder, varValue, StringComparison.OrdinalIgnoreCase);
            }

            return value;
        }

        /// <summary>
        /// Gets the URL encoded value with caching and replaces the URL encode 
        /// placeholder with the URL encoded value.
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
        /// <returns>
        /// The URL encoded value.
        /// </returns>
        private string GetCachedUrlEncodedValue(string placeholder, string value, string url)
        {
            // Try to get from cache first
            if (_placeholderCache.TryGetValue(placeholder, out string? cachedValue))
            {
                Interlocked.Increment(ref _cacheHits);
                return value.Replace(placeholder, cachedValue, StringComparison.OrdinalIgnoreCase);
            }

            Interlocked.Increment(ref _cacheMisses);

            string? encodedValue = HttpUtility.UrlEncode(url);
            if (encodedValue != null)
            {
                // Check cache size and clear if needed
                if (_placeholderCache.Count >= MAX_CACHE_SIZE)
                {
                    Logger.WriteLine(
                        $"Placeholder cache size limit reached ({MAX_CACHE_SIZE}), clearing cache.",
                        LogLevel.DEBUG);
                    _placeholderCache.Clear();
                }

                // Cache the resolved value
                _placeholderCache.TryAdd(placeholder, encodedValue);
                return value.Replace(placeholder, encodedValue, StringComparison.OrdinalIgnoreCase);
            }

            return value;
        }
    }
}
