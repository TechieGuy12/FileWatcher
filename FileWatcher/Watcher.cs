using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TE.FileWatcher.Configuration;
using TE.FileWatcher.Configuration.Notifications;

namespace TE.FileWatcher
{
    /// <summary>
    /// The class that manages the file system watcher for a specific watch.
    /// </summary>
    internal class Watcher : IDisposable, IAsyncDisposable
    {
        // To detect redundant calls
        private bool _disposed = false;

        // The file system watcher object
        private FileSystemWatcher _fsWatcher = new FileSystemWatcher();

        // The set of full path to the folders to ignore
        private HashSet<string> _folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // The set of full path to the paths to ignore
        private HashSet<string> _paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the <see cref="Watch"/> object associated with this watcher.
        /// </summary>
        public Watch Watch { get; private set; }

        /// <summary>
        /// Initializes an instance of the <see cref="Watcher"/> when provided
        /// with the <see cref="Watch"/> object.
        /// </summary>
        /// <param name="watch">
        /// The file watch object.
        /// </param>
        /// <param name="notifications">
        /// The notifications object.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the <paramref name="watch"/> parameter is null.
        /// </exception>
        /// <exception cref="FileWatcherException">
        /// Thrown when the file system watcher could not be initialized.
        /// </exception>
        public Watcher(Watch watch)
        {
            Watch = watch ?? throw new ArgumentNullException(nameof(watch));
            Initialize();
        }

        /// <summary>
        /// Public implementation of Dispose pattern callable by consumers.
        /// </summary>
        public void Dispose() => Dispose(true);

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // Dispose managed state (managed objects).
                _fsWatcher?.Dispose();               
            }

            _fsWatcher = null;
            _disposed = true;
        }

        /// <summary>
        /// Implementation of the DisposeAsync method.
        /// </summary>
        /// <returns>
        /// The ValueTask.
        /// </returns>
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore();

            Dispose(disposing: false);
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
            GC.SuppressFinalize(this);
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
        }

        /// <summary>
        /// Implementation of the DisposeAsyncCore method.
        /// </summary>
        /// <returns>
        /// The ValueTask.
        /// </returns>
        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (_fsWatcher is IAsyncDisposable disposable)
            {
                await disposable.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                _fsWatcher?.Dispose();
            }

            _fsWatcher = null;
        }

        /// <summary>
        /// Initalizes the class.
        /// </summary>
        /// <exception cref="FileWatcherException">
        /// Thrown when the file system watcher could not be initialized.
        /// </exception>
        private void Initialize()
        {
            IsPathValid(Watch.Path);
            GetFolders();
            GetPaths();
            CreateWatcher();
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
            if (Watch == null)
            {
                return;
            }

            if (!IsPathValid(Watch.Path))
            {
                return;
            }

            foreach (string folder in Watch.Exclusions.Folders.Name)
            {
                string folderPath = Path.Combine(Watch.Path, folder);
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
            if (Watch == null)
            {
                return;
            }

            if (!IsPathValid(Watch.Path))
            {
                return;
            }

            foreach (string path in Watch.Exclusions.Paths.Path)
            {
                string fullPath = Path.Combine(Watch.Path, path);
                _paths.Add(fullPath);
            }
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
        /// Creates the watcher that will monitor the specified path.
        /// </summary>
        private void CreateWatcher()
        {
            _fsWatcher = new FileSystemWatcher(Watch.Path);

            _fsWatcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;

            _fsWatcher.Changed += OnChanged;
            _fsWatcher.Created += OnCreated;
            _fsWatcher.Deleted += OnDeleted;
            _fsWatcher.Renamed += OnRenamed;
            _fsWatcher.Filter = "*.*";
            _fsWatcher.IncludeSubdirectories = true;
            _fsWatcher.EnableRaisingEvents = true;
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
            {
                return;
            }

            if (Ignore(e))
            {
                return;
            }

            SendNotification(NotificationTriggers.Change, $"Changed: {e.FullPath}.");
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Created)
            {
                return;
            }

            if (Ignore(e))
            {
                return;
            }

            SendNotification(NotificationTriggers.Change, $"Created: {e.FullPath}.");
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Deleted)
            {
                return;
            }

            if (Ignore(e))
            {
                return;
            }

            SendNotification(NotificationTriggers.Change, $"Deleted: {e.FullPath}.");
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Renamed)
            {
                return;
            }

            if (Ignore(e))
            {
                return;
            }

            SendNotification(NotificationTriggers.Change, $"Renamed: {e.OldFullPath} to {e.FullPath}.");
        }

        /// <summary>
        /// Sends the notification request.
        /// </summary>
        /// <param name="trigger">
        /// The trigger associated with the request.
        /// </param>
        /// <param name="message">
        /// The message to include in the request.
        /// </param>
        private void SendNotification(NotificationTriggers trigger, string message)
        {
            if (Watch.Notifications == null)
            {
                return;
            }

            foreach (Notification notification in Watch.Notifications.NotificationList)
            {
                if (notification.Triggers.NotificationTriggers.HasFlag(trigger))
                {
                    notification.QueueRequest(message);
                }
            }
        }

        /// <summary>
        /// Returns the flag indicating if the change is to be ignored.
        /// </summary>
        /// <param name="args">
        /// The change arguments.
        /// </param>
        /// <returns>
        /// True if the change is to be ignored, otherwise false.
        /// </returns>
        private bool Ignore(FileSystemEventArgs args)
        {
            return
                IgnoreFile(args.Name) ||
                IgnoreFolder(args.FullPath) ||
                IgnoreAttribute(args.FullPath) ||
                IgnorePath(args.FullPath);
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
        private bool IgnoreFile(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            return Watch.Exclusions.Files.Name.Contains(name);
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
        private bool IgnoreAttribute(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            if (!File.Exists(path))
            {
                return false;
            }

            bool hasAttribute = false;
            FileAttributes fileAttributes = File.GetAttributes(path);
            foreach (FileAttributes attribute in Watch.Exclusions.Attributes.Attribute)
            {
                if (fileAttributes.HasFlag(attribute))
                {
                    hasAttribute = true;
                    break;
                }
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
        private bool IgnoreFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            // The path may or may not contain a file, so compare both the full
            // path and the path with the file/last folder removed to determine
            // if it should be ignored
            return _folders.Contains(path) || _folders.Contains(Path.GetDirectoryName(path));
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
        public bool IgnorePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            { 
                return false;
            }

            return _paths.Contains(path);
        }
    }
}
