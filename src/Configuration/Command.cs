﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Xml.Serialization;
using TE.FileWatcher.Log;

namespace TE.FileWatcher.Configuration
{
    /// <summary>
    /// A command to run for a change.
    /// </summary>
    public class Command : RunnableBase, IDisposable
    {
        // The process that will run the command
        private Process? _process;

        // A queue containing the information to start the command process
        private ConcurrentQueue<ProcessStartInfo>? _processInfo;

        // Flag indicating that a process is running
        private bool _isProcessRunning;

        // Flag indicating the class is disposed
        private bool _disposed;

        /// <summary>
        /// Gets or sets the arguments associated with the file to execute.
        /// </summary>
        [XmlElement("arguments")]
        public string? Arguments { get; set; }

        /// <summary>
        /// Gets or sets the full path to the file to be executed.
        /// </summary>
        [XmlElement("path")]
        public string? Path { get; set; }

        /// <summary>
        /// Gets or sets the working directory for the command.
        /// </summary>
        [XmlElement("workingDirectory")]
        public string? WorkingDirectory { get; set; }

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
        public override void Run(ChangeInfo change, TriggerType trigger)
        {
            try
            {
                base.Run(change, trigger);
            }
            catch (ArgumentNullException e)
            {
                Logger.WriteLine(e.Message);
                return;
            }
            catch (InvalidOperationException e)
            {
                Logger.WriteLine(e.Message);
                return;
            }
            catch (FileWatcherTriggerNotMatchException)
            {
                return;
            }

            
            Logger.WriteLine($"Waiting for {WaitBefore} milliseconds.", LogLevel.DEBUG);
            Thread.Sleep(WaitBefore);

            string? commandPath = GetCommand();
            string? arguments = GetArguments();
            string? workingDirectory = GetWorkingDirectory();

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

            _processInfo ??= new ConcurrentQueue<ProcessStartInfo>();

            ProcessStartInfo startInfo = new()
            {
                FileName = commandPath,
                RedirectStandardOutput = true
            };

            if (arguments != null)
            {
                startInfo.Arguments = arguments;
            }

            if (workingDirectory != null)
            {
                startInfo.WorkingDirectory = workingDirectory;
            }

            _processInfo.Enqueue(startInfo);

            // Execute the next process in the queue
            Execute();
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
                _process?.Dispose();
            }

            _disposed = true;
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
                        Logger.WriteLine($"Starting process {startInfo.FileName} {startInfo.Arguments}.");
                        _process = new Process
                        {
                            StartInfo = startInfo
                        };
                        _process.StartInfo.CreateNoWindow = true;
                        _process.StartInfo.UseShellExecute = false;
                        _process.EnableRaisingEvents = true;
                        _process.Exited += OnProcessExit;
                        _process.OutputDataReceived += OnOutputDataReceived;                        
                        _isProcessRunning = _process.Start();
                        _process.BeginOutputReadLine();
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
                else
                {
                    OnCompleted(this, new TaskEventArgs(true, null, $"Completed all commands."));
                }
            }
            catch (Exception ex)
                when (ex is ArgumentNullException || ex is InvalidOperationException || ex is PlatformNotSupportedException || ex is System.ComponentModel.Win32Exception || ex is ObjectDisposedException)
            {
                if (_process != null)
                {
                    Logger.WriteLine(
                        $"Could not run the command '{_process.StartInfo.FileName} {_process.StartInfo.Arguments}'. Reason: {ex.Message}",
                        LogLevel.ERROR);
                    _process.Close();
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
        /// <returns>
        /// The command path string value.
        /// </returns>
        private string? GetArguments()
        {
            if (string.IsNullOrWhiteSpace(Arguments) || Change == null)
            {
                return null;
            }

            return Placeholder.ReplacePlaceholders(
                Arguments,
                Change.WatchPath,
                Change.FullPath,
                Change.OldPath,
                _variables);
        }

        /// <summary>
        /// Gets the command path value by replacing any placeholders with the
        /// actual string values.
        /// </summary>
        /// <returns>
        /// The command path string value.
        /// </returns>
        private string? GetCommand()
        {
            if (string.IsNullOrWhiteSpace(Path) || Change == null)
            {
                return null;
            }

            return Placeholder.ReplacePlaceholders(
                Path,
                Change.WatchPath,
                Change.FullPath,
                Change.OldPath,
                _variables);
        }

        /// <summary>
        /// Gets the working directory path value by replacing any placeholders
        /// with the actual string values.
        /// </summary>
        /// <returns>
        /// The working directory path string value.
        /// </returns>
        private string? GetWorkingDirectory()
        {
            if (string.IsNullOrWhiteSpace(WorkingDirectory) || Change == null)
            {
                return null;
            }

            return Placeholder.ReplacePlaceholders(
                WorkingDirectory,
                Change.WatchPath,
                Change.FullPath,
                Change.OldPath,
                _variables);
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

        /// <summary>
        /// Write the output of the command to the log file.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The event arguments.
        /// </param>
        private void OnOutputDataReceived(object? sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Logger.WriteLine($"{e.Data}");
            }            
        }
    }
}
