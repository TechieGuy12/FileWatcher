using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public static class Logger
    {
        // The queue of log messages
        private static ConcurrentQueue<Message> queue;

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
        /// Initializes an instance of the <see cref="Logger"/> class.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the logger could not be initialized.
        /// </exception>
        static Logger()
        {
            LogPath = Path.GetTempPath();
            LogName = "fw.log";

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
                throw new ArgumentNullException("The logPath argument is null or empty.");
            }

            if (string.IsNullOrWhiteSpace(logName))
            {
                throw new ArgumentNullException("The logName argument is null or empty.");
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
        public static void SetFullPath(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                throw new ArgumentNullException("The fullPath argument is null or empty.");
            }

            // Separate the path and log name so each can be check to ensure
            // they are valid
            string path = Path.GetDirectoryName(fullPath);
            string name = Path.GetFileName(fullPath);

            if (!IsFolderValid(path))
            {
                throw new IOException($"The directory name '{path}' is not valid.");
            }

            if (!IsFileNameValid(name))
            {
                throw new IOException($"The log file name '{name}' is not valid");
            }

            // Store the path, name and the full path if all the checks pass
            LogPath = path;
            LogName = name;
            LogFullPath = Path.Combine(path, name);
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
        /// Writes a line from the queue to the log file.
        /// </summary>
        private static void WriteToLog()
        {
            while (queue.TryDequeue(out Message message))
            {
                try
                {                    
                    using (StreamWriter writer = new StreamWriter(LogFullPath, true))
                    {
                        writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message.LevelString} {message.Value}");
                    }
                }
                catch (Exception ex)
                {
                    Message error = new Message($"Couldn't write to the log. Reason: {ex.Message}", LogLevel.WARNING);
                    Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {error.LevelString} {error.Value}");
                }
            }
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
    }
}