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
    /// <summary>
    /// A base abstract class for the classes which require execution on the
    /// machine and includes placeholders in the data that need to be replaced.
    /// </summary>
    public abstract class RunnableBase : PlaceholderBase
    {        
        /// <summary>
        /// Gets or sets the number of milliseconds to wait before running.
        /// </summary>
        [XmlElement("waitbefore")]
        public int WaitBefore { get; set; }

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
    }
}
