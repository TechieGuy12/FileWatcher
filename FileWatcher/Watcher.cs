using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
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

        private ChangeInfo _lastChange;

        private DateTime _lastWriteTime;

        private Timer _timer;

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
            Logger.WriteLine($"Watcher ended for {Watch.Path}.");
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
            _timer = new Timer(600000);
            _timer.Enabled = true;
            _timer.Elapsed += OnElapsed;
        }

        /// <summary>
        /// Creates the watcher that will monitor the specified path.
        /// </summary>
        private void CreateWatcher()
        {
            Logger.WriteLine($"Creating watcher for {Watch.Path}.");

            _fsWatcher = new FileSystemWatcher(Watch.Path);

            _fsWatcher.NotifyFilter = //NotifyFilters.Attributes
                                 //| NotifyFilters.CreationTime
                                 NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 //| NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite;
                                 //| NotifyFilters.Security
                                 //| NotifyFilters.Size;

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


            ChangeInfo change = GetChange(TriggerType.Change, e.Name, e.FullPath);
            if (change != null)
            {
                Watch.ProcessChange(change);
            }
            
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

            ChangeInfo change = GetChange(TriggerType.Create, e.Name, e.FullPath);
            if (change != null)
            {
                Watch.ProcessChange(change);
            }
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

            ChangeInfo change = GetChange(TriggerType.Delete, e.Name, e.FullPath);
            if (change != null)
            {
                Watch.ProcessChange(change);
            }
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

            ChangeInfo change = GetChange(TriggerType.Rename, e.Name, e.FullPath);
            if (change != null)
            {
                Watch.ProcessChange(change);
            }
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
            if (e.GetException().GetType() == typeof(InternalBufferOverflowException))
            {
                Logger.WriteLine(
                    $"File System Watcher internal buffer overflow.",
                    LogLevel.ERROR);
            }
            else
            {
                Logger.WriteLine(
                    $"An error occurred while watching the file system. Exception: {e.GetException().Message}",
                    LogLevel.ERROR);
            }
            NotAccessibleError(_fsWatcher, e);
        }

        /// <summary>
        /// Called when the timers elapsed time has been reached.
        /// </summary>
        /// <param name="source">
        /// The timer object.
        /// </param>
        /// <param name="e">
        /// The information associated witht he elapsed time.
        /// </param>
        private void OnElapsed(object source, ElapsedEventArgs e)
        {
            _fsWatcher.EnableRaisingEvents = false;
            _fsWatcher.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Gets the <see cref="ChangeInfo"/> object associated with the change.
        /// This method will mark related changes as invalid - such as multiple
        /// changes related to a file copy - to avoid any duplicate work being
        /// done on a file or folder.
        /// </summary>
        /// <param name="trigger">
        /// The type of change.
        /// </param>
        /// <param name="name">
        /// The name of the file or folder.
        /// </param>
        /// <param name="fullPath">
        /// The full path of the file or folder.
        /// </param>
        /// <returns>
        /// The <see cref="ChangeInfo"/> object of the change, otherwise <c>null</c>.
        /// </returns>
        private ChangeInfo GetChange(TriggerType trigger, string name, string fullPath)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(fullPath))
            {
                return null;
            }
            
            try
            {
                // Ignore folder changes as those would be valid for other
                // change events
                if (Directory.Exists(fullPath) && trigger == TriggerType.Change)
                {
                    return null;
                }

                // The flag indicating the change is a valid change and not one
                // derived from a previous change
                bool isValid = true;

                // The current information on the change
                ChangeInfo change = new ChangeInfo(trigger, name, fullPath);

                // The last write time of the file
                DateTime writeTime = default;

                // Verify the file exists before attempting to get the last
                // write time
                if (File.Exists(change.FullPath))
                {
                    writeTime = File.GetLastWriteTime(change.FullPath);
                }

                // Check if the change is related to the same file as the last
                // change that was captured
                if (_lastChange != null && _lastChange.FullPath.Equals(change.FullPath))
                {
                    // If the last change was a copy, then this change is
                    // associated with that change as a copy raises multiple
                    // change events for a file - a copy, and several change
                    // events - so mark this change event as invalid
                    if (_lastChange.Trigger == TriggerType.Create)
                    {
                        isValid = false;
                    }

                    // Check if both the last change was a change, and the
                    // current change is also a change, and the write times
                    // are the same. If all conditions are met, then this indicates
                    // the change being made was associated with another action,
                    // such as a copy, and not an actual change made by the user,
                    // so flag the change as not valid.
                    if ((_lastChange.Trigger == TriggerType.Change && trigger ==TriggerType.Change) &&
                        _lastWriteTime.Equals(writeTime))
                    {
                        isValid = false;
                    }
                }

                // Store the last change and write time for this change
                _lastChange = change;
                _lastWriteTime = writeTime;

                // Return the change if it is valid, or null if the change
                // isn't valid
                return isValid ? change : null;
            }
            catch
            {
                return null;
            }
        }

        private void NotAccessibleError(FileSystemWatcher source, ErrorEventArgs e)
        {
            source.EnableRaisingEvents = false;
            int iMaxAttempts = 120;
            int iTimeOut = 30000;
            int i = 0;
            while (source.EnableRaisingEvents == false && i < iMaxAttempts)
            {
                i += 1;
                try
                {
                    source.EnableRaisingEvents = true;
                }
                catch
                {
                    source.EnableRaisingEvents = false;
                    System.Threading.Thread.Sleep(iTimeOut);
                }
            }

        }
    }
}
