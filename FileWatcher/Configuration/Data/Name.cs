using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TE.FileWatcher.Configuration.Data
{
    [XmlRoot(ElementName = "name")]
    public class Name
    {
        private const string MATCH_TYPE_CONTAINS = "contains";
        private const string MATCH_TYPE_EXACT = "exact";

        /// <summary>
        /// The type of match.
        /// </summary>
        public enum MatchType
        {
            /// <summary>
            /// An exact match.
            /// </summary>
            Exact,
            /// <summary>
            /// A match that contains the value.
            /// </summary>
            Contains
        }

        [XmlText]
        public string Value { get; set; }

        [XmlAttribute("match")]
        public string Match { get; set; } = MATCH_TYPE_EXACT;

        public MatchType GetMatchType()
        {
            if (Match.Equals(MATCH_TYPE_CONTAINS, StringComparison.OrdinalIgnoreCase))
            {
                return MatchType.Contains;
            }
            else
            {
                return MatchType.Exact;
            }
        }
    }
}
