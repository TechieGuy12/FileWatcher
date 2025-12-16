using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Timers;
using System.Xml.Serialization;
using TE.FileWatcher.Log;

namespace TE.FileWatcher.Configuration
{
    /// <summary>
    /// The watch element in the XML file.
    /// </summary>
    public class Watch : HasNeedsBase, IDisposable
    {
        // Timer interval for resetting FileSystemWatcher (10 minutes)
        private const int FILESYSTEMWATCHER_RESET_INTERVAL_MS = 600000;

        // Sleep time when queue is empty (milliseconds)
        private const int EMPTY_QUEUE_SLEEP_MS = 100;

        // Sleep time for path existence check (milliseconds)
        private const int PATH_CHECK_SLEEP_MS = 500;

        // Maximum retry attempts for FileSystemWatcher recovery
        private const int WATCHER_RECOVERY_MAX_ATTEMPTS = 120;

        // Timeout between FileSystemWatcher recovery attempts (30 seconds)
        private const int WATCHER_RECOVERY_TIMEOUT_MS = 30000;

        // The file system watcher object
        private FileSystemWatcher? _fsWatcher;

        // Information about the last change
        private ChangeInfo? _lastChange;

        // The write time for the last change
        private DateTime _lastWriteTime;

        // Lock object for thread-safe access to _lastChange and _lastWriteTime
        private readonly object _changeLock = new object();

        // The timer used to "reset" the FileSystemWatch object
        private System.Timers.Timer? _timer;

        // The background worker that processes the file/folder changes
        private BackgroundWorker? _worker;

        // The queue that will contain the changes
        private ConcurrentQueue<ChangeInfo>? _queue;

        // Flag to indicate to ignore the next change as a Create trigger will
        // generate two Change triggers
        private bool _ignoreNextChange = false;

        // Flag indicating the class is disposed
        private bool _disposed;

        // Cached ID string for logging to avoid repeated null-coalescing
        private string? _idLogStringCache;

        /// <summary>
        /// Gets the ID if one is specified for the watch, otherwise, return the
        /// watch path.
        /// </summary>
        [XmlIgnore]
        private string? IdLogString => _idLogStringCache ??= (Id ?? Path);
    
        /// <summary>
        /// Gets or sets the id of the watch.
        /// </summary>
        [XmlElement(ElementName = "id", IsNullable = true)]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the path of the watch.
        /// </summary>
        [XmlElement("path", IsNullable = false)]
        public string? Path { get; set; }

        /// <summary>
        /// Gets or sets the timeout value (in seconds) for the watch.
        /// </summary>
        [XmlElement("timeout")]
        public int Timeout { get; set; }

        /// <summary>
        /// Gets or sets the filters.
        /// </summary>
        [XmlElement("filters")]
        public Filters? Filters { get; set; }

        /// <summary>
        /// Gets or sets the exclusions.
        /// </summary>
        [XmlElement("exclusions")]
        public Exclusions? Exclusions { get; set; }

        /// <summary>
        /// Gets or sets the notifications for the watch.
        /// </summary>
        [XmlElement("notifications")]
        public Notifications? Notifications { get; set; }

        /// <summary>
        /// Gets or sets the actions for the watch.
        /// </summary>
        [XmlElement("actions")]
        public Actions? Actions { get; set; }

        /// <summary>
        /// Gets or sets the commands for the watch.
        /// </summary>
        [XmlElement("commands")]
        public Commands? Commands { get; set; }

        /// <summary>
        /// Gets or sets the workflows for the watch.
        /// </summary>
        [XmlElement("workflows")]
        public Workflows? Workflows { get; set; }

        /// <summary>
        /// Gets the flag indicating the watch is running.
        /// </summary>
        [XmlIgnore]
        public bool IsActive
        {
            get
            {
                return (_fsWatcher != null && _fsWatcher.EnableRaisingEvents);
            }
        }
    
        /// <summary>
        /// Add the watch variable list to the dependent tasks.
        /// </summary>
        private void AddVariables(ConcurrentDictionary<string, string>? variables)
        {
            Variables?.Add(variables);

            if (Notifications != null)
            {
                Notifications.Variables ??= new Variables();
                Notifications.Variables.Add(Variables?.AllVariables);
            }

            if (Actions != null)
            {
                Actions.Variables ??= new Variables();
                Actions.Variables.Add(Variables?.AllVariables);
            }

            if (Commands != null)
            {
                Commands.Variables ??= new Variables();
                Commands.Variables.Add(Variables?.AllVariables);
            }

            if (Workflows != null)
            {
                Workflows.Variables ??= new Variables();
                Workflows.Variables.Add(Variables?.AllVariables);
            }
        }

        /// <summary>
        /// Processes the file or folder change.
        /// </summary>
        /// <param name="change">
        /// Information about the change.
        /// </param>
        /// <param name="trigger">
        /// The type of change.
        /// </param>
        public override void Run(ChangeInfo change, TriggerType trigger)
        {
            if (change == null || _queue == null || _worker == null)
            {
                if (Logger.LogLevel <= LogLevel.DEBUG)
                {
                    Logger.WriteLine($"{IdLogString}: Run called but prerequisites not met. change null: {change == null}, queue null: {_queue == null}, worker null: {_worker == null}. (Watch.Run)", LogLevel.DEBUG);
                }
                return;
            }

            _queue.Enqueue(change);
            
            if (Logger.LogLevel <= LogLevel.DEBUG)
            {
                Logger.WriteLine($"[{change.CorrelationId}] {IdLogString}: Enqueued {trigger} event for {change.FullPath}. Queue count: {_queue.Count}. (Watch.Run)", LogLevel.DEBUG);
            }

            if (!_worker.IsBusy)
            {
                if (Logger.LogLevel <= LogLevel.DEBUG)
                {
                    Logger.WriteLine($"{IdLogString}: Starting background worker to process queue. (Watch.Run)", LogLevel.DEBUG);
                }
                _worker.RunWorkerAsync();
            }
            else
            {
                if (Logger.LogLevel <= LogLevel.DEBUG)
                {
                    Logger.WriteLine($"{IdLogString}: Background worker already busy, event will be processed in current run. (Watch.Run)", LogLevel.DEBUG);
                }
            }
        }

        /// <summary>
        /// Starts the watch.
        /// </summary>
        public bool Start(Collection<Watch> watches, ConcurrentDictionary<string, string>? variables)
        {
            if (_fsWatcher != null || _timer != null)
            {
                Stop();
            }

            if (!IsInitialized)
            {
                AddVariables(variables);
            }

            if (PathExists())
            {
                CreateFileSystemWatcher();
                CreateQueue();
                CreateBackgroundWorker();
                CreateTimer();                
                SetNeedWatch(watches);
                Initialize();

                Logger.WriteLine($"{IdLogString}: Number of needs: {_needs?.Count ?? 0}. (Watch.Start)", LogLevel.DEBUG);
            }
            else
            {
                Logger.WriteLine($"{IdLogString}: The path '{Path}' does not exists, so the watch was not created.");
            }

            return IsActive;
        }

        /// <summary>
        /// Stops the watch.
        /// </summary>
        public bool Stop()
        {
            if (_disposed)
            {
                return !IsActive;
            }

            // Properly dispose resources before setting to null
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }

            if (_worker != null)
            {
                _worker.Dispose();
                _worker = null;
            }

            if (_fsWatcher != null)
            {
                _fsWatcher.EnableRaisingEvents = false;
                _fsWatcher.Dispose();
                _fsWatcher = null;
            }

            // Clear the queue
            _queue = null;

            return !IsActive;
        }

        /// <summary>
        /// Releases all resources used by the class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Release all resources used by the class.
        /// </summary>
        /// <param name="disposing">
        /// Indicates the whether the class is disposing.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _timer?.Dispose();
                _worker?.Dispose();
                _fsWatcher?.Dispose();
            }

            _disposed = true;
        }


        /// <summary>
        /// Create the background worker to process the changes.
        /// </summary>
        private void CreateBackgroundWorker()
        {
            _worker = new BackgroundWorker
            {
                WorkerSupportsCancellation = false
            };
            _worker.DoWork += DoWork;
        }

        /// <summary>
        /// Create the FileSystemWatcher object.
        /// </summary>
        private void CreateFileSystemWatcher()
        {
            if (string.IsNullOrWhiteSpace(Path))
            {
                Logger.WriteLine(
                    "The path to watch was not specified.",
                    LogLevel.ERROR);
                return;
            }

            Logger.WriteLine($"{IdLogString}: Creating watch. Path: {Path}.");

            _fsWatcher = new FileSystemWatcher(Path)
            {
                NotifyFilter =
                    NotifyFilters.Attributes
                    | NotifyFilters.CreationTime
                    | NotifyFilters.DirectoryName
                    | NotifyFilters.FileName                    
                    | NotifyFilters.LastWrite
                    | NotifyFilters.Security
                    | NotifyFilters.Size
            };

            _fsWatcher.Changed += OnChanged;
            _fsWatcher.Created += OnCreated;
            _fsWatcher.Deleted += OnDeleted;
            _fsWatcher.Renamed += OnRenamed;
            _fsWatcher.Error += OnError;
            _fsWatcher.Filter = "*.*";
            _fsWatcher.IncludeSubdirectories = true;
            _fsWatcher.EnableRaisingEvents = true;

            Logger.WriteLine($"{IdLogString}: Watch created.");
        }

        /// <summary>
        /// Create the queue of changes to be processed.
        /// </summary>
        private void CreateQueue()
        {
            _queue = new ConcurrentQueue<ChangeInfo>();
        }

        /// <summary>
        /// Create the timer to reset the FileSystemWatcher object.
        /// </summary>
        private void CreateTimer()
        {
            _timer = new System.Timers.Timer(FILESYSTEMWATCHER_RESET_INTERVAL_MS);
            _timer.Enabled = true;
            _timer.Elapsed += OnElapsed;
        }

        /// <summary>
        /// Process the changes in a background worker thread.
        /// </summary>
        /// <param name="sender">
        /// The object associated with this event.
        /// </param>
        /// <param name="e">
        /// Arguments associated with the background worker.
        /// </param>
        private void DoWork(object? sender, DoWorkEventArgs e)
        {
            ProcessChange();
        }

        public void ProcessChange()
        {
            if (string.IsNullOrWhiteSpace(Path))
            {
                return;
            }

            _queue ??= new ConcurrentQueue<ChangeInfo>();            

            if (_queue.IsEmpty)
            {
                Initialize();
                Thread.Sleep(EMPTY_QUEUE_SLEEP_MS);
                return;
            }

            // Peek at the queue to get correlation IDs for logging
            var queueSnapshot = _queue.ToArray();
            var firstCorrelationId = queueSnapshot.Length > 0 ? (Guid?)queueSnapshot[0].CorrelationId : null;
            var correlationPrefix = firstCorrelationId.HasValue ? $"[{firstCorrelationId.Value}] " : "";

            // Guard expensive DEBUG logging to avoid string allocations
            if (Logger.LogLevel <= LogLevel.DEBUG)
            {
                // Include first correlation ID in watch-level logs for context
                Logger.WriteLine(
                    $"{correlationPrefix}{IdLogString}: ProcessChange started. CanRun: {CanRun}, IsRunning: {IsRunning}, Queue count: {_queue.Count}. (Watch.ProcessChange)",
                    LogLevel.DEBUG);
                
                // Detailed dependency information
                if (_needs != null && _needs.Count > 0)
                {
                    var needsStatus = string.Join(", ", _needs.Select(n => 
                        $"{(n is Watch w ? w.Id ?? w.Path : "unknown")}: {(n.HasCompleted ? "completed" : "pending")}"));
                    Logger.WriteLine(
                        $"{correlationPrefix}{IdLogString}: Dependency status - Count: {_needs.Count}, Details: [{needsStatus}]. (Watch.ProcessChange)",
                        LogLevel.DEBUG);
                }
                else
                {
                    Logger.WriteLine(
                        $"{correlationPrefix}{IdLogString}: No dependencies configured. (Watch.ProcessChange)",
                        LogLevel.DEBUG);
                }
            }
            
            if (!CanRun || IsRunning)
            {
                if (Logger.LogLevel <= LogLevel.DEBUG)
                {
                    var reason = !CanRun ? "dependencies not met" : "already running";
                    Logger.WriteLine(
                        $"{correlationPrefix}{IdLogString}: Watch blocked from running. Reason: {reason}. Queue will be processed when watch becomes available. (Watch.ProcessChange)",
                        LogLevel.DEBUG);
                }
                return;
            }

            var startTime = DateTime.Now;
            int processedCount = 0;
            var correlationIds = new List<Guid>();

            if (!_queue.IsEmpty)
            {
                // Include first correlation ID in the "Starting tasks" message
                var message = firstCorrelationId.HasValue 
                    ? $"[{firstCorrelationId.Value}] {IdLogString}: Starting tasks for watch. Path {Path}."
                    : $"{IdLogString}: Starting tasks for watch. Path {Path}.";
                OnStarted(this, new TaskEventArgs(true, IdLogString, message));
            }

            while (!_queue.IsEmpty)
            {
                if (_queue.TryDequeue(out ChangeInfo? change))
                {
                    if (change != null)
                    {
                        correlationIds.Add(change.CorrelationId);
                        
                        if (Logger.LogLevel <= LogLevel.DEBUG)
                        {
                            // Change-specific log WITH correlation ID
                            Logger.WriteLine(
                                $"[{change.CorrelationId}] {IdLogString}: Processing item {processedCount + 1} of current batch. Remaining in queue: {_queue.Count}. (Watch.ProcessChange)", 
                                LogLevel.DEBUG);
                        }
                        
                        ProcessSingleChange(change);
                        processedCount++;
                    }
                    else
                    {
                        if (Logger.LogLevel <= LogLevel.DEBUG)
                        {
                            Logger.WriteLine($"{IdLogString}: Dequeued null change object. (Watch.ProcessChange)", LogLevel.DEBUG);
                        }
                    }
                }
                else
                {
                    if (Logger.LogLevel <= LogLevel.DEBUG)
                    {
                        Logger.WriteLine($"{IdLogString}: Failed to dequeue change from queue. (Watch.ProcessChange)", LogLevel.DEBUG);
                    }
                }
            }

            if (Logger.LogLevel <= LogLevel.DEBUG && processedCount > 0)
            {
                var duration = DateTime.Now - startTime;
                var idsString = processedCount == 1 
                    ? correlationIds[0].ToString() 
                    : $"{correlationIds.Count} items: " + string.Join(", ", correlationIds.Select(id => id.ToString().Substring(0, 8)));
                
                // Include correlation context in completion log
                Logger.WriteLine(
                    $"[{correlationIds[0]}] {IdLogString}: ProcessChange completed in {duration.TotalMilliseconds:F2}ms. Processed: {idsString}. (Watch.ProcessChange)",
                    LogLevel.DEBUG);
            }

            // Include first correlation ID in "Tasks completed" message
            var completionMessage = firstCorrelationId.HasValue
                ? $"[{firstCorrelationId.Value}] {IdLogString}: Tasks completed for watch."
                : $"{IdLogString}: Tasks completed for watch.";
            OnCompleted(this, new TaskEventArgs(true, IdLogString, completionMessage));
        }

        /// <summary>
        /// Processes a single change by checking filters, exclusions, and executing workflows.
        /// </summary>
        /// <param name="change">The change to process.</param>
        private void ProcessSingleChange(ChangeInfo change)
        {
            if (Logger.LogLevel <= LogLevel.DEBUG)
            {
                Logger.WriteLine(
                    $"[{change.CorrelationId}] {IdLogString}: Change: {change.FullPath}, {change.Trigger} (Watch.ProcessChange)",
                    LogLevel.DEBUG);
            }

            if (!PassesFilters(change))
            {
                if (Logger.LogLevel <= LogLevel.DEBUG)
                {
                    Logger.WriteLine(
                        $"[{change.CorrelationId}] {IdLogString}: Change filtered out: {change.FullPath}",
                        LogLevel.DEBUG);
                }
                return;
            }

            if (!PassesExclusions(change))
            {
                if (Logger.LogLevel <= LogLevel.DEBUG)
                {
                    Logger.WriteLine(
                        $"[{change.CorrelationId}] {IdLogString}: Change excluded: {change.FullPath}",
                        LogLevel.DEBUG);
                }
                return;
            }

            if (Logger.LogLevel <= LogLevel.DEBUG)
            {
                Logger.WriteLine(
                    $"[{change.CorrelationId}] {IdLogString}: Started: {change.FullPath}, {change.Trigger} (Watch.ProcessChange)",
                    LogLevel.DEBUG);
            }

            ExecuteWorkflows(change);

            if (Logger.LogLevel <= LogLevel.DEBUG)
            {
                Logger.WriteLine(
                    $"[{change.CorrelationId}] {IdLogString}: Completed: {change.FullPath}, {change.Trigger} (Watch.ProcessChange)",
                    LogLevel.DEBUG);
            }
        }

        /// <summary>
        /// Checks if the change passes the configured filters.
        /// </summary>
        /// <param name="change">The change to check.</param>
        /// <returns>True if the change passes filters or no filters are configured; otherwise false.</returns>
        private bool PassesFilters(ChangeInfo change)
        {
            if (Filters == null || !Filters.IsSpecified())
            {
                return true;
            }

            // If the file or folder is not a match, then don't take any further actions
            return Filters.IsMatch(change);
        }

        /// <summary>
        /// Checks if the change passes the configured exclusions.
        /// </summary>
        /// <param name="change">The change to check.</param>
        /// <returns>True if the change is not excluded; otherwise false.</returns>
        private bool PassesExclusions(ChangeInfo change)
        {
            if (Exclusions == null || !Exclusions.IsSpecified())
            {
                return true;
            }

            // If the file or folder is in the exclude list, then don't take any further actions
            return !Exclusions.Exclude(change);
        }

        /// <summary>
        /// Executes all configured workflows, notifications, actions, and commands for the change.
        /// </summary>
        /// <param name="change">The change to execute workflows for.</param>
        private void ExecuteWorkflows(ChangeInfo change)
        {
            Workflows?.Run(change, change.Trigger);
            Notifications?.Send(change.Trigger, change);
            Actions?.Run(change.Trigger, change);
            Commands?.Run(change.Trigger, change);
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
        private ChangeInfo? GetChange(TriggerType trigger, string? name, string fullPath)
        {
            return GetChange(trigger, name, fullPath, null, null);
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
        /// <param name="oldName">
        /// The old name of the file or folder.
        /// </param>
        /// <param name="oldPath">
        /// The old path of the file or folder.
        /// </param>
        /// <returns>
        /// The <see cref="ChangeInfo"/> object of the change, otherwise <c>null</c>.
        /// </returns>
        private ChangeInfo? GetChange(TriggerType trigger, string? name, string fullPath, string? oldName, string? oldPath)
        {
            if (string.IsNullOrWhiteSpace(Path) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(fullPath))
            {
                if (Logger.LogLevel <= LogLevel.DEBUG)
                {
                    Logger.WriteLine($"{IdLogString}: GetChange rejected - invalid parameters. Path null/empty: {string.IsNullOrWhiteSpace(Path)}, name null/empty: {string.IsNullOrWhiteSpace(name)}, fullPath null/empty: {string.IsNullOrWhiteSpace(fullPath)}. (Watch.GetChange)", LogLevel.DEBUG);
                }
                return null;
            }

            try
            {
                // Ignore folder changes as those would be valid for other
                // change events
                if (Directory.Exists(fullPath) && trigger == TriggerType.Change)
                {
                    if (Logger.LogLevel <= LogLevel.DEBUG)
                    {
                        Logger.WriteLine($"{IdLogString}: Ignoring folder Change event for: {fullPath}. (Watch.GetChange)", LogLevel.DEBUG);
                    }
                    return null;
                }

                // The flag indicating the change is a valid change and not one
                // derived from a previous change
                bool isValid = true;
                string? invalidReason = null;

                // The current information on the change
                ChangeInfo change = new(trigger, Path, name, fullPath, oldName, oldPath);

                // The last write time of the file
                DateTime writeTime = default;

                // Optimize: GetLastWriteTime returns DateTime.MinValue for non-existent files
                // This eliminates the need for File.Exists check, reducing I/O by 50%
                try
                {
                    writeTime = File.GetLastWriteTime(change.FullPath);
                }
                catch (IOException ex)
                {
                    if (Logger.LogLevel <= LogLevel.DEBUG)
                    {
                        Logger.WriteLine($"[{change.CorrelationId}] {IdLogString}: IOException getting last write time for {fullPath}: {ex.Message}. (Watch.GetChange)", LogLevel.DEBUG);
                    }
                    writeTime = default;
                }
                catch (UnauthorizedAccessException ex)
                {
                    if (Logger.LogLevel <= LogLevel.DEBUG)
                    {
                        Logger.WriteLine($"[{change.CorrelationId}] {IdLogString}: Access denied getting last write time for {fullPath}: {ex.Message}. (Watch.GetChange)", LogLevel.DEBUG);
                    }
                    writeTime = default;
                }

                // Check if the change is related to the same file as the last
                // change that was captured
                lock (_changeLock)
                {
                    if (_lastChange != null && _lastChange.FullPath.Equals(change.FullPath, StringComparison.OrdinalIgnoreCase))
                    {
                        // If the last change was a copy, then this change is
                        // associated with that change as a copy raises multiple
                        // change events for a file - a copy, and several change
                        // events - so mark this change event as invalid
                        if (_lastChange.Trigger == TriggerType.Create || _ignoreNextChange)
                        {
                            isValid = false;
                            invalidReason = $"Duplicate after Create event (last: {_lastChange.Trigger}, ignoreNext: {_ignoreNextChange})";

                            // Set the flag to ignore a second Change Trigger only
                            // if the previous change was a Create
                            _ignoreNextChange = (_lastChange.Trigger == TriggerType.Create);
                        }

                        // Check if both the last change was a change, and the
                        // current change is also a change, and the write times
                        // are the same. If all conditions are met, then this indicates
                        // the change being made was associated with another action,
                        // such as a copy, and not an actual change made by the user,
                        // so flag the change as not valid.
                        if ((_lastChange.Trigger == TriggerType.Change && trigger == TriggerType.Change) &&
                            _lastWriteTime.Equals(writeTime))
                        {
                            isValid = false;
                            invalidReason = $"Duplicate Change with same timestamp ({writeTime:yyyy-MM-dd HH:mm:ss.fff})";
                        }
                    }

                    if (Logger.LogLevel <= LogLevel.DEBUG)
                    {
                        if (isValid)
                        {
                            Logger.WriteLine($"[{change.CorrelationId}] {IdLogString}: Accepting {trigger} event for {name}. WriteTime: {writeTime:yyyy-MM-dd HH:mm:ss.fff}. (Watch.GetChange)", LogLevel.DEBUG);
                        }
                        else
                        {
                            Logger.WriteLine($"[{change.CorrelationId}] {IdLogString}: Filtering out {trigger} event for {name}. Reason: {invalidReason}. (Watch.GetChange)", LogLevel.DEBUG);
                        }
                    }

                    // Store the last change and write time for this change
                    _lastChange = change;
                    _lastWriteTime = writeTime;
                }

                // Return the change if it is valid, or null if the change
                // isn't valid
                return isValid ? change : null;
            }
            catch (Exception ex)
            {
                Logger.WriteLine(
                    $"{IdLogString}: Error processing change for '{fullPath}'. Reason: {ex.Message} (Watch.GetChange)",
                    LogLevel.WARNING);
                return null;
            }
        }

        /// <summary>
        /// Reset the FileSystemWatcher object by disabling and attempting to
        /// re-enable the event listening for the object.
        /// </summary>
        /// <param name="source">
        /// The source.
        /// </param>
        /// <param name="e">
        /// The event arguments related to the exception.
        /// </param>
        private static void NotAccessibleError(FileSystemWatcher source, ErrorEventArgs e)
        {
            source.EnableRaisingEvents = false;
            int attemptCount = 0;
            while (source.EnableRaisingEvents == false && attemptCount < WATCHER_RECOVERY_MAX_ATTEMPTS)
            {
                attemptCount++;
                try
                {
                    source.EnableRaisingEvents = true;
                }
                catch (Exception ex)
                {
                    source.EnableRaisingEvents = false;
                    Logger.WriteLine(
                        $"FileSystemWatcher recovery attempt {attemptCount}/{WATCHER_RECOVERY_MAX_ATTEMPTS} failed. Reason: {ex.Message}",
                        LogLevel.WARNING);
                    Thread.Sleep(WATCHER_RECOVERY_TIMEOUT_MS);
                }
            }

            if (!source.EnableRaisingEvents)
            {
                Logger.WriteLine(
                    $"FileSystemWatcher recovery failed after {WATCHER_RECOVERY_MAX_ATTEMPTS} attempts.",
                    LogLevel.ERROR);
            }
            else
            {
                Logger.WriteLine(
                    $"FileSystemWatcher recovered successfully after {attemptCount} attempts.",
                    LogLevel.INFO);
            }
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

            ChangeInfo? change = GetChange(TriggerType.Change, e.Name, e.FullPath);
            if (change != null)
            {
                Run(change, TriggerType.Change);
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

            ChangeInfo? change = GetChange(TriggerType.Create, e.Name, e.FullPath);       

            if (change != null)
            {
                Run(change, TriggerType.Create);
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

            ChangeInfo? change = GetChange(TriggerType.Delete, e.Name, e.FullPath);
            if (change != null)
            {
                Run(change, TriggerType.Delete);
            }
        }

        /// <summary>
        /// Called when the timers elapsed time has been reached. The timer is
        /// used because the FileSystemWatcher object tends to stop raising
        /// events after a period of time. After the elapsed time, this method
        /// will disable and then re-enable event raising to sort of "reset" 
        /// the FilesSystemWatcher and prevent it from stop listening to
        /// events.
        /// </summary>
        /// <param name="source">
        /// The timer object.
        /// </param>
        /// <param name="e">
        /// The information associated with the elapsed time.
        /// </param>
        private void OnElapsed(object? source, ElapsedEventArgs e)
        {
            if (_fsWatcher != null)
            {
                _fsWatcher.EnableRaisingEvents = false;
                _fsWatcher.EnableRaisingEvents = true;
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

            ChangeInfo? change = GetChange(TriggerType.Rename, e.Name, e.FullPath, e.OldName, e.OldFullPath);
            if (change != null)
            {
                Run(change, TriggerType.Rename);
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
                    $"{IdLogString}: File System Watcher internal buffer overflow.",
                    LogLevel.ERROR);
            }
            else
            {
                Logger.WriteLine(
                    $"{IdLogString}: An error occurred while watching the file system. Exception: {e.GetException().Message}",
                    LogLevel.ERROR);
            }

            if (_fsWatcher != null)
            {
                NotAccessibleError(_fsWatcher, e);
            }
        }

        /// <summary>
        /// Waits for a specified amount of time before the path to watch
        /// exists. The time value is provided by the <see cref="Timeout"/>
        /// property.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the path exists, otherwise <c>false</c>.
        /// </returns>
        private bool PathExists()
        {
            // Calculate the total number of times the thread will wait based
            // on the timeout value and the PATH_CHECK_SLEEP_MS
            int waitTime = (Timeout * 1000) / PATH_CHECK_SLEEP_MS;

            // The number of times the thread has slept
            int checkCount = 0;

            while (!Directory.Exists(Path) && waitTime > checkCount)
            {
                Thread.Sleep(PATH_CHECK_SLEEP_MS);
                checkCount++;
            }

            return Directory.Exists(Path);
        }

        private void SetNeedWatch(Collection<Watch> watches)
        {
            if (watches == null || watches.Count <= 0)
            {                
                return;
            }

            if (Needs == null || Needs.Length <= 0)
            {
                return;
            }

            for (int i = 0; i < Needs.Length; i++)
            {
                Watch? needWatch = watches
                    .Where(w => w.Id != null)
                    .FirstOrDefault(w => w.Id == Needs[i]);
                if (needWatch != null)
                {
                    Logger.WriteLine($"{IdLogString}: Needs {needWatch.Id}. (Watch.SetNeedWatch)", LogLevel.DEBUG);
                    SetNeed(needWatch);
                }
            }
        }

        public override void OnCompleted(object? sender, TaskEventArgs e)
        {
            base.OnCompleted(sender, e);
        }

        public override void OnNeedsCompleted(object? sender, TaskEventArgs e)
        {
            Logger.WriteLine(
                $"{IdLogString}: Needed watch {e.Id} completed. Checking if this watch can run. (Watch.OnNeedsCompleted)",
                LogLevel.DEBUG);

            base.OnNeedsCompleted(sender, e);

            ProcessChange();
        }   

    }
}
