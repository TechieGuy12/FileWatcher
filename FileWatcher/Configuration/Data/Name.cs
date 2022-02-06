using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace TE.FileWatcher.Configuration.Data
{
    /// <summary>
    /// Methods and properties that manage a name value.
    /// </summary>
    [XmlRoot(ElementName = "name")]
    public class Name
    {
        // The regular expression
        private Regex? _regex;

        /// <summary>
        /// Gets or sets the name pattern to match.
        /// </summary>
        [XmlText]
        public string? Pattern { get; set; }

        /// <summary>
        /// Checks to see if the <see cref="Pattern"/> property provides a
        /// match for the <c>value</c> parameter.
        /// </summary>
        /// <param name="value">
        /// The value to compare with the <see cref="Pattern"/> property.
        /// </param>
        /// <returns>
        /// <c>true</c> if the value is a match for the pattern, otherwise
        /// <c>false</c>.
        /// </returns>
        public bool IsMatch(string value)
        {
            if (string.IsNullOrWhiteSpace(Pattern))
            {
                return false;
            }

            bool isMatch = value.Equals(Pattern);
            if (!isMatch)
            {
                isMatch = value.Contains(Pattern);
            }

            if (!isMatch)
            {
                isMatch = PatternMatcher.StrictMatchPattern(
                    Pattern.ToUpper(CultureInfo.InvariantCulture),
                    value.ToUpper(CultureInfo.InvariantCulture));
            }

            if (!isMatch)
            {
                try
                {
                    if (_regex == null)
                    {
                        string escapedPattern = Pattern.Replace(@"\", @"\\");
                        _regex = new Regex(escapedPattern, RegexOptions.IgnoreCase);
                    }
                    isMatch = _regex.IsMatch(value);
                }
                catch
                {
                    isMatch = false;
                }
            }

            return isMatch;
        }
    }
}
