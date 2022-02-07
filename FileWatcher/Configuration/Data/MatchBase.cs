using System.Xml.Serialization;
using TE.FileWatcher.Logging;

namespace TE.FileWatcher.Configuration.Data
{
    /// <summary>
    /// A base class containing the properties and methods for filtering the
    /// files and folders of the watch.
    /// </summary>
    public abstract class MatchBase
    {
        // The set of full path to the folders to ignore
        private protected HashSet<string>? _folders;

        // The set of full path to the paths to ignore
        private protected HashSet<string>? _paths;

        // Sets the flag indicating the ignore lists have been populated
        private protected bool _initialized = false;

        // The path associated with the watch
        private protected string? _watchPath;

        /// <summary>
        /// Gets or sets the files node.
        /// </summary>
        [XmlElement("files")]
        public Files? Files { get; set; }

        /// <summary>
        /// Gets or sets the folders node.
        /// </summary>
        [XmlElement("folders")]
        public Folders? Folders { get; set; }

        /// <summary>
        /// Gets or sets the paths node.
        /// </summary>
        [XmlElement("paths")]
        public Paths? Paths { get; set; }

        /// <summary>
        /// Gets or sets the attributes node.
        /// </summary>
        [XmlElement("attributes")]
        public Attributes? Attributes { get; set; }

        /// <summary>
        /// Gets or sets the type of filter used for logging.
        /// </summary>
        [XmlIgnore]
        private protected string FilterTypeName { get; set; } = "Filter";

        /// <summary>
        /// Gets a value indicating if at least one valid filtering value has
        /// been specified. An empty element could be added to the XML file,
        /// so this method ensures a filtering element has a valid value
        /// specified.
        /// </summary>
        /// <returns>
        /// <c>true</c> if at least one filtering value is specified, otherwise
        /// <c>false</c>.
        /// </returns>
        public bool IsSpecified()
        {
            bool isSpecified = false;
            if (Files != null && Files.Name.Count > 0)
            {
                isSpecified = true;
            }

            if (Folders != null && Folders.Name.Count > 0)
            {
                isSpecified = true;
            }

            if (Attributes != null && Attributes.Attribute.Count > 0)
            {
                isSpecified = true;
            }

            if (Paths != null && Paths.Path.Count > 0)
            {
                isSpecified = true;
            }

            return isSpecified;
        }

        /// <summary>
        /// Returns the flag indicating whether the attribute for a file that
        /// is changed matches the attributes from the configuration file.
        /// When a file is deleted, the attributes of the file cannot be checked
        /// since the file is no longer available, so the attributes cannot be
        /// determined, so on deletion this function will always return 
        /// <c>false</c>.
        /// </summary>
        /// <param name="path">
        /// The full path to the file.
        /// </param>
        /// <returns>
        /// True if the file change is a match, otherwise false.
        /// </returns>
        private protected bool AttributeMatch(string path)
        {
            if (Attributes == null || Attributes.Attribute.Count <= 0)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            if (!File.Exists(path) && !Directory.Exists(path))
            {
                return false;
            }

            bool hasAttribute = false;
            try
            {                 
                FileAttributes fileAttributes = File.GetAttributes(path);
                foreach (FileAttributes attribute in Attributes.Attribute)
                {
                    if (fileAttributes.HasFlag(attribute))
                    {
                        Logger.WriteLine($"{FilterTypeName}: The path '{path}' has the attribute '{attribute}'.");
                        hasAttribute = true;
                        break;
                    }
                }
            }
            catch
            {
                hasAttribute = false;
            }
            return hasAttribute;
        }

        /// <summary>
        /// Returns the flag indicating whether the current file changed is
        /// a match that is found for files.
        /// </summary>
        /// <param name="name">
        /// The name of the file.
        /// </param>
        /// <returns>
        /// True if the file change is a match, otherwise false.
        /// </returns>
        private protected bool FileMatch(string name)
        {
            if (Files == null || Files.Name.Count <= 0)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            bool isMatch = false;
            foreach (Name fileName in Files.Name)
            {
                isMatch = fileName.IsMatch(name);
                if (isMatch)
                {
                    Logger.WriteLine($"{FilterTypeName}: The match pattern '{fileName.Pattern}' is a match for file {name}.");
                    break;
                }
            }

            return isMatch;
        }

        /// <summary>
        /// Returns the flag indicating whether the current folder change is a
        /// match when reporting the change.
        /// </summary>
        /// <param name="path">
        /// The path of the folder that was changed.
        /// </param>
        /// <returns>
        /// True if the folder change is a match, otherwise false.
        /// </returns>
        private protected bool FolderMatch(string path)
        {
            if (Folders == null || Folders.Name.Count <= 0)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            bool isMatch = false;
            foreach (Name folder in Folders.Name)
            {
                isMatch = folder.IsMatch(path);
                if (isMatch)
                {
                    Logger.WriteLine($"{FilterTypeName}: The match pattern '{folder.Pattern}' is a match for folder '{path}'.");
                    break;
                }
            }

            return isMatch;
        }

        /// <summary>
        /// Returns the flag indicating whether the current path change is a
        /// match when reporting the change.
        /// </summary>
        /// <param name="path">
        /// The full path.
        /// </param>
        /// <returns>
        /// True if the path change is a match, otherwise false.
        /// </returns>
        private protected bool PathMatch(string path)
        {
            if (_paths == null || _paths.Count <= 0)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            bool isMatch = false;
            foreach (string aPath in _paths)
            {
                if (path.Contains(aPath))
                {
                    Logger.WriteLine($"{FilterTypeName}: The path '{path}' contains the path '{aPath}'.");
                    isMatch = true;
                    break;
                }
            }

            return isMatch;
        }

        /// <summary>
        /// The folder paths stored in the <see cref="Watch"/> class are
        /// relative to the <see cref="Watch.Path"/> property. To compare
        /// the folders, the relative folder location are combined with
        /// the <see cref="Watch.Path"/> to create the absolute path of 
        /// the folders. This is then used to compare with any folder that
        /// is changed.
        /// </summary>
        /// <exception cref="FileWatcherException">
        /// Thrown when there is a problem with the path.
        /// </exception>
        private void GetFolders()
        {
            if (Folders == null)
            {
                return;
            }

            if (!IsPathValid(_watchPath))
            {
                return;
            }

            foreach (Name folderName in Folders.Name)
            {
                if (_watchPath != null && folderName.Pattern != null)
                {
                    folderName.Pattern = 
                        Path.Combine(_watchPath, folderName.Pattern);
                }
            }
        }

        /// <summary>
        /// The paths stored in the <see cref="Watch"/> class are relative to 
        /// the <see cref="Watch.Path"/> property. To compare the paths, the
        /// relative folder location are combined with the <see cref="Watch.Path"/>
        /// to create the absolute path of each specified path value. This is
        /// then used to compare with any path that is changed.
        /// </summary>
        /// <exception cref="FileWatcherException">
        /// Thrown when there is a problem with the path.
        /// </exception>
        private void GetPaths()
        {
            if (Paths == null)
            {
                return;
            }

            if (!IsPathValid(_watchPath))
            {
                return;
            }

            _paths = new HashSet<string>(
                Paths.Path.Count,
                StringComparer.OrdinalIgnoreCase);

            foreach (string path in Paths.Path)
            {
                if (_watchPath != null)
                {
                    string fullPath = Path.Combine(_watchPath, path);
                    _paths.Add(fullPath);
                }
            }
        }

        /// <summary>
        /// Initialize the values in the exclusion lists. 
        /// </summary>
        /// <param name="watchPath">
        /// The path to watch.
        /// </param>
        /// <exception cref="FileWatcherException">
        /// Thrown when there is a problem with the path.
        /// </exception>
        private protected void Initialize(string watchPath)
        {
            _watchPath = watchPath;

            IsPathValid(_watchPath);
            GetFolders();
            GetPaths();

            _initialized = true;
        }

        /// <summary>
        /// Gets a value indicating if a match is found between the changed
        /// file/folder data, and the specified patterns.
        /// </summary>
        /// <param name="watchPath">
        /// The path being watched.
        /// </param>
        /// <param name="name">
        /// The name of the changed file or folder.
        /// </param>
        /// <param name="fullPath">
        /// The full path to the changed file or folder.
        /// </param>
        /// <returns>
        /// <c>true</c> of a match is found, otherwise <c>false</c>.
        /// </returns>
        /// <exception cref="FileWatcherException">
        /// Thrown when there is a problem with the path.
        /// </exception>
        private protected bool IsMatchFound(string watchPath, string name, string fullPath)
        {
            if (string.IsNullOrWhiteSpace(watchPath) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(fullPath))
            {
                return false;
            }

            if (!_initialized)
            {
                Initialize(watchPath);
            }

            bool isMatch = false;
            if (Files != null && Files.Name.Count > 0)
            {
                isMatch |= FileMatch(name);
            }

            if (Folders != null && Folders.Name.Count > 0)
            {
                isMatch |= FolderMatch(fullPath);
            }

            if (Attributes != null && Attributes.Attribute.Count > 0)
            {
                isMatch |= AttributeMatch(fullPath);
            }

            if (Paths != null && Paths.Path.Count > 0)
            {
                isMatch |= PathMatch(fullPath);
            }

            return isMatch;
        }

        /// <summary>
        /// Checks to ensure the provided path is valid.
        /// </summary>
        /// <param name="path"></param>
        /// <exception cref="FileWatcherException">
        /// Thrown when there is a problem with the path.
        /// </exception>
        private protected static bool IsPathValid(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new FileWatcherException("The path is null or empty.");
            }

            if (!Directory.Exists(path))
            {
                throw new FileWatcherException($"The path '{path}' does not exist.");
            }

            return true;
        }
    }
}
