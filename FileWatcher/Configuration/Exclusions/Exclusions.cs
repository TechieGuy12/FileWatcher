﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TE.FileWatcher.Logging;

namespace TE.FileWatcher.Configuration.Exclusions
{
    /// <summary>
    /// An exclusions node in the XML file.
    /// </summary>
    public class Exclusions
    {
        // The set of full path to the folders to ignore
        private HashSet<string> _folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // The set of full path to the paths to ignore
        private HashSet<string> _paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Sets the flag indicating the ignore lists have been populated
        private bool _initialized = false;

        // The path associated with the watch
        private string _watchPath;

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

            foreach (string folder in Folders.Name)
            {
                string folderPath = Path.Combine(_watchPath, folder);
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

            foreach (string path in Paths.Path)
            {
                string fullPath = Path.Combine(_watchPath, path);
                _paths.Add(fullPath);
            }
        }

        /// <summary>
        /// Returns the flag indicating whether the current file changed should
        /// be ignored when reporting the change.
        /// </summary>
        /// <param name="name">
        /// The name of the file.
        /// </param>
        /// <returns>
        /// True if the file change is to be ignored, otherwise false.
        /// </returns>
        private bool ExcludeFile(string name)
        {
            if (Files == null || Files.Name.Count <= 0)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            if (Files.Name.Contains(name))
            {
                Logger.WriteLine($"The file name '{name}' is set to be excluded.");
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the flag indicating whether the attribute for a file that
        /// is change should cause the reporting of the change to be ignored.
        /// When a file is deleted, the attributes of the file cannot be checked
        /// since the file is no longer availalbe, so the attributes cannot be
        /// determined, so on deletion this function will always return 
        /// <c>true</c>.
        /// </summary>
        /// <param name="path">
        /// The full path to the file.
        /// </param>
        /// <returns>
        /// True if the file change is to be ignored, otherwise false.
        /// </returns>
        private bool ExcludeAttribute(string path)
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
                        Logger.WriteLine($"The path '{path}' as the attribute '{attribute}'.");
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
        /// Returns the flag indicating whether the current folder change should
        /// be ignored when reporting the change.
        /// </summary>
        /// <param name="path">
        /// The path of the folder that was changed.
        /// </param>
        /// <returns>
        /// True if the folder change is to be ignored, otherwise false.
        /// </returns>
        private bool ExcludeFolder(string path)
        {
            if (_folders == null || _folders.Count <= 0)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            bool exclude = false;
            foreach (string folder in _folders)
            {
                if (path.Contains(folder) || Path.GetDirectoryName(path).Contains(folder))
                {
                    Logger.WriteLine($"The path '{path}' contains the folder '{folder}'.");
                    exclude = true;
                    break;
                }
            }

            return exclude;
        }

        /// <summary>
        /// Returns the flag indicating whether the current path change should
        /// be ignored when reporting the change.
        /// </summary>
        /// <param name="path">
        /// The full path.
        /// </param>
        /// <returns>
        /// True if the path change is to be ignored, otherwise false.
        /// </returns>
        private bool ExcludePath(string path)
        {
            if (_paths == null || _paths.Count <= 0)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            bool exclude = false;
            foreach (string aPath in _paths)
            {
                if (path.Contains(aPath))
                {
                    Logger.WriteLine($"The path '{path}' contains the path '{aPath}'.");
                    exclude = true;
                    break;
                }
            }

            return exclude;
        }

        /// <summary>
        /// Initialize the values in the exclusion lists. 
        /// </summary>
        /// <param name="watchPath"></param>
        private void Initialize(string watchPath)
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
        private bool IsPathValid(string path)
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

        /// <summary>
        /// Returns the flag indicating if the change is to be ignored.
        /// </summary>
        /// <param name="watchPath">
        /// The path associated with the watch.
        /// </param>
        /// <param name="name">
        /// The name of the file or folder.
        /// </param>
        /// <<param name="fullPath">
        /// The full path to the changed file or folder.
        /// </param>
        /// <returns>
        /// True if the change is to be ignored, otherwise false.
        /// </returns>
        public bool Exclude(string watchPath, string name, string fullPath)
        {
            if (string.IsNullOrWhiteSpace(watchPath) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(fullPath))
            {
                return false;
            }

            if (!_initialized)
            {
                Initialize(watchPath);
            }

            return
                ExcludeFile(name) ||
                ExcludeFolder(fullPath) ||
                ExcludeAttribute(fullPath) ||
                ExcludePath(fullPath);
        }
    }
}