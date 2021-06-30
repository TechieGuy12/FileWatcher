using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TE.FileWatcher.Configuration
{
    /// <summary>
    /// The Attributes node in the XML file.
    /// </summary>
    public class Attributes
    {
        /// <summary>
        /// Gets the list of file attributes in string value form.
        /// </summary>
        [XmlElement("attribute")]
        public HashSet<string> AttributeStrings { get; private set; } = new HashSet<string>();

        /// <summary>
        /// Gets the list of file attributes.
        /// </summary>
        [XmlIgnore]
        public HashSet<FileAttributes> Attribute
        {
            get
            {
                HashSet<FileAttributes> attributes = new HashSet<FileAttributes>();
                foreach (string attribute in AttributeStrings)
                {
                    try
                    {
                        FileAttributes fileAttributes =
                            (FileAttributes)Enum.Parse(typeof(FileAttributes), attribute);
                        attributes.Add(fileAttributes);
                    }
                    catch (Exception ex)
                        when (ex is ArgumentNullException || ex is ArgumentException || ex is OverflowException)
                    {
                        continue;
                    }
                }

                return attributes;
            }
        }
    }
}
