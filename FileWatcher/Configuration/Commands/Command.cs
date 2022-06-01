using System.Collections.Concurrent;
using System.Diagnostics;
using System.Xml.Serialization;
using TE.FileWatcher.Logging;

namespace TE.FileWatcher.Configuration.Commands
{
    /// <summary>
    /// A command to run for a change.
    /// </summary>
    public class Command : RunnableBase
    {
        // The process that will run the command
        private Process? _process;

        // A queue containing the information to start the command process
        private ConcurrentQueue<ProcessStartInfo>? _processInfo;

        // Flag indicating that a process is running
        private bool _isProcessRunning = false;

        /// <summary>
        /// Gets or sets the arguments associated with the file to execute.
        /// </summary>
        [XmlElement("arguments")]
        public string? Arguments { get; set; }

        /// <summary>
        /// Gets or sets the full path to the file to executed.
        /// </summary>
        [XmlElement("path")]
        public string? Path { get; set; }

        /// <summary>
        /// Gets or sets the triggers of the action.
        /// </summary>
        [XmlElement("triggers")]
        public Triggers Triggers { get; set; } = new Triggers();

        /// <summary>
        /// Queues the command process to be run.
        /// </summary>
        /// <param name="watchPath">
        /// The watch path.
        /// </param>
        /// <param name="fullPath">
        /// The full path to the changed file or folder.
        /// </param>
        /// <param name="trigger">
        /// The trigger for the command.
        /// </param>
        public override void Run(string watchPath, string fullPath, TriggerType trigger)
        {
            if (string.IsNullOrWhiteSpace(watchPath) || string.IsNullOrWhiteSpace(fullPath))
            {
                return;
            }

            if (Triggers == null || Triggers.TriggerList == null)
            {
                return;
            }

            if (Triggers.TriggerList.Count <= 0 || !Triggers.Current.HasFlag(trigger))
            {
                return;
            }

            string? commandPath = GetCommand(watchPath, fullPath);
            string? arguments = GetArguments(watchPath, fullPath);

            if (string.IsNullOrWhiteSpace(commandPath))
            {
                Logger.WriteLine($"The command was not provided. Command was not run.",
                    LogLevel.ERROR);
                return;
            }

            if (!File.Exists(commandPath))
            {
                Logger.WriteLine(
                    $"The command '{commandPath}' was not found. Command was not run.",
                    LogLevel.ERROR);
                return;
            }

            try
            {
                if (_processInfo == null)
                {
                    _processInfo = new ConcurrentQueue<ProcessStartInfo>();
                }

                ProcessStartInfo startInfo = new()
                {
                    FileName = commandPath
                };

                if (arguments != null)
                {
                    startInfo.Arguments = arguments;
                }

                _processInfo.Enqueue(startInfo);

                // Execute the next process in the queue
                Execute();
            }
            catch (Exception ex)
            {
                Logger.WriteLine(
                    $"Could not run the command '{commandPath} {arguments}'. Reason: {ex.Message}",
                    LogLevel.ERROR);
            }
        }

        /// <summary>
        /// Executes the next command process from the queue.
        /// </summary>
        private void Execute()
        {
            // If the queue is null or empty, then no command is waiting to nbe
            // executed
            if (_processInfo == null || _processInfo.IsEmpty)
            {
                return;
            }

            // If a command is currently running, then don't start another
            if (_isProcessRunning)
            {
                return;
            }

            try
            {
                if (_processInfo.TryDequeue(out ProcessStartInfo? startInfo))
                {
                    if (File.Exists(startInfo.FileName))
                    {
                        _process = new Process
                        {
                            StartInfo = startInfo
                        };
                        _process.StartInfo.CreateNoWindow = true;
                        _process.StartInfo.UseShellExecute = false;
                        _process.EnableRaisingEvents = true;
                        _process.Exited += OnProcessExit;
                        _isProcessRunning = _process.Start();
                    }
                    else
                    {
                        Logger.WriteLine(
                            $"The command '{startInfo.FileName}' was not found. Command was not run.",
                            LogLevel.ERROR);

                        // Execute the next process in the queue
                        Execute();
                    }
                }
            }
            catch (Exception ex)
            {
                if (_process != null)
                {
                    Logger.WriteLine(
                        $"Could not run the command '{_process.StartInfo.FileName} {_process.StartInfo.Arguments}'. Reason: {ex.Message}",
                        LogLevel.ERROR);
                }
                else
                {
                    Logger.WriteLine(
                        $"Could not run the command. Reason: {ex.Message}",
                        LogLevel.ERROR);
                }
            }
        }

        /// <summary>
        /// Gets the arguments value by replacing any placeholders with the
        /// actual string values.
        /// </summary>
        /// <param name="watchPath">
        /// The watch path.
        /// </param>
        /// <param name="fullPath">
        /// The full path of the changed file.
        /// </param>
        /// <returns>
        /// The command path string value.
        /// </returns>
        private string? GetArguments(string watchPath, string fullPath)
        {
            if (string.IsNullOrWhiteSpace(Arguments))
            {
                return null;
            }

            string? arguments = ReplacePlaceholders(Arguments, watchPath, fullPath);
            if (!string.IsNullOrWhiteSpace(arguments))
            {
                arguments = ReplaceDatePlaceholders(arguments, watchPath, fullPath);
            }

            return arguments;
        }

        /// <summary>
        /// Gets the command path value by replacing any placeholders with the
        /// actual string values.
        /// </summary>
        /// <param name="watchPath">
        /// The watch path.
        /// </param>
        /// <param name="fullPath">
        /// The full path of the changed file.
        /// </param>
        /// <returns>
        /// The command path string value.
        /// </returns>
        private string? GetCommand(string watchPath, string fullPath)
        {
            if (string.IsNullOrWhiteSpace(Path))
            {
                return null;
            }

            string? path = ReplacePlaceholders(Path, watchPath, fullPath);
            if (!string.IsNullOrWhiteSpace(path))
            {
                path = ReplaceDatePlaceholders(path, watchPath, fullPath);
            }

            return path;
        }

        /// <summary>
        /// The event that is raised when the process has exied.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="args">
        /// The event arguments.
        /// </param>
        private void OnProcessExit(object? sender, EventArgs args)
        {
            _isProcessRunning = false;

            if (_process == null)
            {
                return;
            }

            Logger.WriteLine($"The execution '{_process.StartInfo.FileName} {_process.StartInfo.Arguments}' has exited. Exit code: {_process.ExitCode}.");
            _process.Dispose();
            _process = null;

            // Execute the next process in the queue
            Execute();
        }
    }
}
