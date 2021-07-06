using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TE.FileWatcher.Configuration;
using TE.FileWatcher.Configuration.Notifications;
using TE.FileWatcher.Logging;

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
            CreateWatcher();
        }

        /// <summary>
        /// Creates the watcher that will monitor the specified path.
        /// </summary>
        private void CreateWatcher()
        {
            Logger.WriteLine($"Creating watcher for {Watch.Path}.");

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
            _fsWatcher.Error += OnError;
            _fsWatcher.Filter = "*.*";
            _fsWatcher.IncludeSubdirectories = true;
            _fsWatcher.EnableRaisingEvents = true;

            Logger.WriteLine($"Watcher created for {Watch.Path}.");
        }

        /// <summary>
        /// Called when a file or folder is changed.
        /// </summary>
        /// <param name="sender">
        /// The object calling the method.
        /// </param>
        /// <param name="e">
        /// The event parameters.
        /// </param>
        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
            {
                return;
            }

            Watch.ProcessChange(NotificationTriggers.Change, e.Name, e.FullPath);
        }

        /// <summary>
        /// Called when a file or folder is created.
        /// </summary>
        /// <param name="sender">
        /// The object calling the method.
        /// </param>
        /// <param name="e">
        /// The event parameters.
        /// </param>
        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Created)
            {
                return;
            }

            Watch.ProcessChange(NotificationTriggers.Create, e.Name, e.FullPath);
        }

        /// <summary>
        /// Called when a file or folder is deleted.
        /// </summary>
        /// <param name="sender">
        /// The object calling the method.
        /// </param>
        /// <param name="e">
        /// The event parameters.
        /// </param>
        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Deleted)
            {
                return;
            }

            Watch.ProcessChange(NotificationTriggers.Delete, e.Name, e.FullPath);
        }

        /// <summary>
        /// Called when a file or folder is renamed.
        /// </summary>
        /// <param name="sender">
        /// The object calling the method.
        /// </param>
        /// <param name="e">
        /// The event parameters.
        /// </param>
        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Renamed)
            {
                return;
            }

            Watch.ProcessChange(NotificationTriggers.Rename, e.Name, e.FullPath);
        }

        /// <summary>
        /// Called when the file system watcher throws an exception.
        /// </summary>
        /// <param name="sender">
        /// The object calling the method.
        /// </param>
        /// <param name="e">
        /// The event parameters.
        /// </param>
        private void OnError(object sender, ErrorEventArgs e)
        {
            Logger.WriteLine(
                $"An error occurred while watching the file system. Exception: {e.GetException().Message}", 
                LogLevel.ERROR);
        }
    }
}
