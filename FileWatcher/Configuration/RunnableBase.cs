using System;
using System.Collections.Generic;
using IO = System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        protected string ReplacePlaceholders(string value, string watchPath, string fullPath)
        {
            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(watchPath) || string.IsNullOrWhiteSpace(fullPath))
            {
                return null;
            }

            string relativeFullPath = GetRelativeFullPath(watchPath, fullPath);
            string relativePath = GetRelativePath(watchPath, fullPath);
            string fileName = GetFilename(fullPath, true);
            string fileNameWithoutExtension = GetFilename(fullPath, false);
            string extension = GetFileExtension(fullPath);

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
    }
}
