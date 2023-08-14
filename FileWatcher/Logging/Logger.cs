using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace TE.FileWatcher.Logging
{
    /// <summary>
    /// The log level of a log message.
    /// </summary>
    public enum LogLevel
    {
        INFO = 0,
        WARNING,
        ERROR,
        FATAL
    }

    /// <summary>
    /// The logger class that contains the properties and methods needed to
    /// manage the log file.
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// The default log file name.
        /// </summary>
        public const string DEFAULT_LOG_NAME = "fw.log";

        // A megabyte - for the purists, this is actually a mebibyte, but let's
        // not split hairs as this is just a log file size after all
        private const int MEGABYTE = 1048576;

        // The queue of log messages
        private static readonly ConcurrentQueue<Message> queue;

        /// <summary>
        /// Gets the path to the log.
        /// </summary>
        public static string LogPath { get; private set; }

        /// <summary>
        /// Gets the name of the log file.
        /// </summary>
        public static string LogName { get; private set; }

        /// <summary>
        /// Gets or sets the full path of the log file.
        /// </summary>
        public static string LogFullPath { get; private set; }

        /// <summary>
        /// Gets or sets the size (in megabytes) of a log file before it is
        /// backed up and a new log file is created.
        /// </summary>
        public static int LogSize { get; private set; }

        /// <summary>
        /// Gets or sets the number of log file to retain.
        /// </summary>
        public static int LogNumber { get; private set; }

        // The object used for the lock
        private static readonly object locker = new();
       
        /// <summary>
        /// Initializes an instance of the <see cref="Logger"/> class.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the logger could not be initialized.
        /// </exception>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        static Logger()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            LogPath = Path.GetTempPath();
            LogName = DEFAULT_LOG_NAME;
            LogSize = Configuration.Logging.DEFAULT_LOG_SIZE;
            LogNumber = Configuration.Logging.DEFAULT_LOG_NUMBER;

            try
            {
                SetFullPath(LogPath, LogName);
            }
            catch (Exception ex)
                when (ex is ArgumentException || ex is ArgumentNullException || ex is IOException)
            {
                throw new InvalidOperationException($"The logger could not be initialized. Reason: {ex.Message}");
            }

            queue = new ConcurrentQueue<Message>();
        }

        /// <summary>
        /// Sets the logger options.
        /// </summary>
        /// <param name="logOptions">
        /// The options for the logger.
        /// </param>
        public static void SetLogger(Configuration.Logging logOptions)
        {
            if (logOptions == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(logOptions.LogPath))
            {
                SetFullPath(logOptions.LogPath);
            }

            LogSize = logOptions.Size;
            LogNumber = logOptions.Number;
        }

        /// <summary>
        /// Adds a message to be added to the log. This method defaults the log
        /// level to <see cref="LogLevel.INFO"/>.
        /// </summary>
        /// <param name="message">
        /// The message to write.
        /// </param>
        public static void WriteLine(string message)
        {
            WriteLine(message, LogLevel.INFO);
        }

        /// <summary>
        /// Adds a message to be added to the log.
        /// </summary>
        /// <param name="message">
        /// The message to write.
        /// </param>
        /// <param name="level">
        /// The log level associated with the message.
        /// </param>
        public static void WriteLine(string message, LogLevel level)
        {
            queue.Enqueue(new Message(message, level));
            WriteToLog();
        }

        /// <summary>
        /// Sets the full path of the logging file using the <paramref name="logPath"/>
        /// and the <paramref name="logName"/> arguments.
        /// </summary>
        /// <param name="logPath">
        /// The path to the log file.
        /// </param>
        /// <param name="logName">
        /// The name of the log file.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when an argument is null or empty.
        /// </exception>
        /// <exception cref="IOException">
        /// Thrown when either the folder or log file name are not valid.
        /// </exception>
        private static void SetFullPath(string logPath, string logName)
        {
            if (string.IsNullOrWhiteSpace(logPath))
            {
                throw new ArgumentNullException(nameof(logPath));
            }

            if (string.IsNullOrWhiteSpace(logName))
            {
                throw new ArgumentNullException(nameof(logName));
            }
            
            string fullPath = Path.Combine(logPath, logName);
            SetFullPath(fullPath);
        }

        /// <summary>
        /// Sets the full path to the log file.
        /// </summary>
        /// <param name="fullPath">
        /// The full path to the log file.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the <paramref name="fullPath"/> argument is null or
        /// empty.
        /// </exception>
        /// <exception cref="IOException">
        /// Thrown when either the folder or log file name are not valid.
        /// </exception>
        private static void SetFullPath(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                throw new ArgumentNullException(nameof(fullPath));
            }

            // Separate the path and log name so each can be check to ensure
            // they are valid
            string? path = Path.GetDirectoryName(fullPath);
            string name = Path.GetFileName(fullPath);

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new IOException($"The directory path is null or empty.");
            }

            if (!IsFolderValid(path))
            {
                throw new IOException($"The directory name '{path}' is not valid.");
            }

            if (!IsFileNameValid(name))
            {
                throw new IOException($"The log file name '{name}' is not valid");
            }

            if (!string.IsNullOrWhiteSpace(path))
            {
                // Store the path, name and the full path if all the checks pass
                LogPath = path;
                LogName = name;
                LogFullPath = Path.Combine(path, name);
            }
            else
            {
                LogPath = string.Empty;
                LogName = name;
                LogFullPath = string.Empty;
            }
        }

        /// <summary>
        /// Checks if the folder provided is a valid folder. The folder will
        /// be created if it is valid.
        /// </summary>
        /// <param name="folder">
        /// The path to the folder.
        /// </param>
        /// <returns>
        /// True if the folder is valid and exists, otherwise false.
        /// </returns>
        private static bool IsFolderValid(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                return false;
            }

            if (Directory.Exists(folder))
            {
                return true;
            }
            else
            {
                // Check for any invalid characters in the folder
                if (folder.IndexOfAny(Path.GetInvalidPathChars()) != -1)
                {
                    return false;
                }

                try
                {
                    Directory.CreateDirectory(folder);
                    return Directory.Exists(folder);
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Checks if the file name is valid.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file.
        /// </param>
        /// <returns>
        /// True if the file name is valid, otherwise false.
        /// </returns>
        private static bool IsFileNameValid(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            // Check for any invalid characters in the file name
            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Rolls over the current log file if the size matches the specfied
        /// max size for the log file.
        /// </summary>
        private static void RolloverLog()
        {
            // Check to ensure the log file exists before trying to get the
            // size of the file
            if (!File.Exists(LogFullPath))
            {
                return;
            }

            try
            {
                // Get and check the log size to see if it is still less than the
                // specified log size
                FileInfo fileInfo = new(LogFullPath);
                if (fileInfo.Length < (LogSize * MEGABYTE))
                {
                    return;
                }

                int totalLogs = LogNumber - 1;

                if (totalLogs > 0)
                {
                    // Loop through the number of specified log files, and then copy
                    // previous log files to the next log number and then delete the
                    // log so the previous one can be copied
                    for (int i = totalLogs; i > 0; i--)
                    {
                        string logFile = LogFullPath + $".{i - 1}";
                        if (File.Exists(logFile))
                        {
                            string nextlogFile = LogFullPath + $".{i}";
                            File.Copy(logFile, nextlogFile, true);
                            File.Delete(logFile);
                        }
                    }

                    // Copy the current log file to the first log number backup and
                    // then delete the log file so it can be recreated
                    string newLogFile = LogFullPath + $".1";
                    File.Copy(LogFullPath, newLogFile, true);
                }

                File.Delete(LogFullPath);
                File.Create(LogFullPath).Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ DateTime.Now:yyyy - MM - dd HH: mm: ss} Could not rollover the log file. Reason: {ex.Message}");
            }
        }

        /// <summary>
        /// Writes a line from the queue to the log file.
        /// </summary>
        private static void WriteToLog()
        {
            while (queue.TryDequeue(out Message? message))
            {
                try
                {
                    lock (locker)
                    {
                        RolloverLog();
                        using StreamWriter writer = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? new(LogFullPath, true, System.Text.Encoding.UTF8) : new(LogFullPath, true);
                        writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message.LevelString} {message.Value}");
                    }
                }
                catch (Exception ex)
                {
                    Message error = new($"Couldn't write to the log. Reason: {ex.Message}", LogLevel.WARNING);
                    Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {error.LevelString} {error.Value}");
                }
            }
        }
    }
}