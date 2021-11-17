using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TE.FileWatcher.Logging;

namespace TE.FileWatcher.Configuration.Data
{
    /// <summary>
    /// A base class containing the properties and methods for filtering the
    /// files and folders of the watch.
    /// </summary>
    public abstract class FilterBase
    {
        // The set of full path to the folders to ignore
        private protected HashSet<string> _folders;

        // The set of full path to the paths to ignore
        private protected HashSet<string> _paths;

        // Sets the flag indicating the ignore lists have been populated
        private protected bool _initialized = false;

        // The path associated with the watch
        private protected string _watchPath;

        /// <summary>
        /// Gets or sets the files node.
        /// </summary>
        [XmlElement("files")]
        public Files Files { get; set; }

        /// <summary>
        /// Gets or sets the folders node.
        /// </summary>
        [XmlElement("folders")]
        public Folders Folders { get; set; }

        /// <summary>
        /// Gets or sets the paths node.
        /// </summary>
        [XmlElement("paths")]
        public Paths Paths { get; set; }

        /// <summary>
        /// Gets or sets the attributes node.
        /// </summary>
        [XmlElement("attributes")]
        public Attributes Attributes { get; set; }

        /// <summary>
        /// Gets or sets the type of filter used for logging.
        /// </summary>
        [XmlIgnore]
        private protected string FilterTypeName { get; set; } = "Filter";

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
                if (fileName.GetMatchType() == Name.MatchType.Contains)
                {
                    if (name.Contains(fileName.Value))
                    {
                        isMatch = true;
                    }
                }
                else
                {
                    if (name.Equals(fileName.Value))
                    {
                        isMatch = true;
                    }
                }

                if (isMatch)
                {
                    Logger.WriteLine($"{FilterTypeName}: The file name '{fileName.Value}' is a match for file {name}.");
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
            if (_folders == null || _folders.Count <= 0)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            bool isMatch = false;
            foreach (string folder in _folders)
            {
                if (path.Contains(folder) || Path.GetDirectoryName(path).Contains(folder))
                {
                    Logger.WriteLine($"{FilterTypeName}: The path '{path}' contains the folder '{folder}'.");
                    isMatch = true;
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

            _folders = new HashSet<string>(
                Folders.Name.Count,
                StringComparer.OrdinalIgnoreCase);

            //foreach (string folder in Folders.Name)
            foreach (Name folderName in Folders.Name)
            {
                string folderPath = Path.Combine(_watchPath, folderName.Value);
                _folders.Add(folderPath);
            }
        }

        /// <summary>
        /// The paths stored in the <see cref="Watch"/> class are relative to 
        /// the <see cref="Watch.Path"/> property. To compare the paths, the
        /// relative folder location are combined with the <see cref="Watch.Path"/>
        /// to create the absolute path of each specified path value. This is
        /// then used to compare with any path that is changed.
        /// </summary>
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
                string fullPath = Path.Combine(_watchPath, path);
                _paths.Add(fullPath);
            }
        }

        /// <summary>
        /// Initialize the values in the exclusion lists. 
        /// </summary>
        /// <param name="watchPath"></param>
        private protected void Initialize(string watchPath)
        {
            _watchPath = watchPath;

            IsPathValid(_watchPath);
            GetFolders();
            GetPaths();

            _initialized = true;
        }

        /// <summary>
        /// Checks to ensure the provided path is valid.
        /// </summary>
        /// <param name="path"></param>
        /// <exception cref="FileWatcherException">
        /// Thrown when there is a problem with the path.
        /// </exception>
        private protected bool IsPathValid(string path)
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
