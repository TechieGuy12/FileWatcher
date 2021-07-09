using System;
using System.Collections.Generic;
using IO = System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TE.FileWatcher.Logging;
using TE.FileWatcher.FileSystem;

namespace TE.FileWatcher.Configuration.Actions
{
    /// <summary>
    /// The Action to perform during a watch event.
    /// </summary>
    public class Action
    {
        // The full path placeholder
        private const string PLACEHOLDER_FULLPATH = "[fullpath]";
        // The path placholder
        private const string PLACEHOLDER_PATH = "[path]";
        // The file placeholder
        private const string PLACEHOLDER_FILE = "[file]";
        // The file name placeholder
        private const string PLACEHOLDER_FILENAME = "[filename]";
        // The file extension placeholder
        private const string PLACEHOLDER_EXTENSION = "[extension]";

        // The placeholders for the destination path
        private Dictionary<string, string> _destinationPlaceholders 
            = new Dictionary<string, string>();

        // The placeholders for the source path
        private Dictionary<string, string> _sourcePlaceholders
            = new Dictionary<string, string>();

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
        public string Destination { get; set; }

        /// <summary>
        /// Gets or sets the verify flag.
        /// </summary>
        [XmlElement(ElementName = "verify", DataType = "boolean")]
        public bool Verify { get; set; }

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
        private string GetRelativeFullPath(string watchPath, string fullPath)
        {
            if (string.IsNullOrWhiteSpace(watchPath) || string.IsNullOrWhiteSpace(fullPath))
            {
                return fullPath;
            }

            try
            {
                int index = fullPath.IndexOf(watchPath, StringComparison.OrdinalIgnoreCase);
                return (index < 0) ? fullPath : fullPath.Remove(index, watchPath.Length);
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
        private string GetRelativePath(string watchPath, string fullPath)
        {
            string relativeFullPath = IO.Path.GetDirectoryName(fullPath);
            return GetRelativeFullPath(watchPath, relativeFullPath);
        }

        /// <summary>
        /// Gets the name of the file with or without the extension.
        /// </summary>
        /// <param name="fullPath">
        /// The full path to the file.
        /// </param>
        /// <returns>
        /// The name of the file, otherwise <c>null</c>.
        /// </returns>
        private string GetFilename(string fullPath, bool includeExtension)
        {
            if (string.IsNullOrEmpty(fullPath) || !IO.File.Exists(fullPath))
            {
                return null;
            }

            return includeExtension ? IO.Path.GetFileNameWithoutExtension(fullPath) : IO.Path.GetFileName(fullPath);
        }

        /// <summary>
        /// Gets the file extension.
        /// </summary>
        /// <param name="fullPath">
        /// The full path to the file.
        /// </param>
        /// <returns>
        /// The extension of the full, otherwise <c>null</c>.
        /// </returns>
        private string GetFileExtension(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath) || !IO.File.Exists(fullPath))
            {
                return null;
            }

            return IO.Path.GetExtension(fullPath);
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
        private string GetDestination(string watchPath, string fullPath)
        {
            string relativeFullPath = GetRelativeFullPath(watchPath, fullPath);
            string relativePath = GetRelativePath(watchPath, fullPath);
            string fileName = GetFilename(fullPath, true);
            string fileNameWithoutExtension = GetFilename(fullPath, false);
            string extension = GetFileExtension(fullPath);

            string destination = Destination;
            destination = destination.Replace(PLACEHOLDER_FULLPATH, relativeFullPath);
            destination = destination.Replace(PLACEHOLDER_PATH, relativePath);
            destination = destination.Replace(PLACEHOLDER_FILENAME, fileName);
            destination = destination.Replace(PLACEHOLDER_FILE, fileNameWithoutExtension);
            destination = destination.Replace(PLACEHOLDER_EXTENSION, extension);

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
        private string GetSource(string watchPath, string fullPath)
        {
            if (string.IsNullOrWhiteSpace(Source))
            {
                Source = PLACEHOLDER_FULLPATH;
            }

            string source = Source;
            source = source.Replace(PLACEHOLDER_FULLPATH, fullPath);

            return source;
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
        public void Run(string watchPath, string fullPath)
        {
            if (string.IsNullOrWhiteSpace(watchPath) || string.IsNullOrWhiteSpace(fullPath))
            {
                return;
            }

            string source = GetSource(watchPath, fullPath);
            string destination = GetDestination(watchPath, fullPath);

            try
            {
                switch (Type)
                {
                    case ActionType.Copy:
                        if (File.IsValid(source))
                        {
                            File.Copy(source, destination, Verify);
                            Logger.WriteLine($"Copied {source} to {destination}.");
                        }
                        break;
                    case ActionType.Move:
                        if (File.IsValid(source))
                        {
                            File.Move(source, destination, Verify);
                            Logger.WriteLine($"Moved {source} to {destination}.");
                        }
                        break;
                    case ActionType.Delete:
                        if (File.IsValid(source))
                        {
                            File.Delete(source);
                            Logger.WriteLine($"Deleted {source}.");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                string message = (ex.InnerException == null) ? ex.Message : ex.InnerException.Message;
                Logger.WriteLine($"Could not {Type.ToString().ToLower()} file {source}. Reason: {message}");
                return;
            }
        }
    }
}
