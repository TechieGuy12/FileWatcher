using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TE.FileWatcher.Configuration;
using TE.FileWatcher.Logging;
using TE.FileWatcher.Notifications;

namespace TE.FileWatcher
{
    /// <summary>
    /// The main program class.
    /// </summary>
    class Program
    {
        // Success return code
        private const int SUCCESS = 0;

        // Error return code
        private const int ERROR = -1;

        // The default configuration file name
        const string DEFAULT_CONFIG_FILE = "config.xml";

        // The default notifications file name
        const string DEFAULT_NOTIFICATION_FILE = "notification.xml";

        /// <summary>
        /// The main function.
        /// </summary>
        /// <param name="args">
        /// Arguments passed into the application.
        /// </param>
        /// <returns>
        /// Returns 0 on success, otherwise non-zero.
        /// </returns>
        static int Main(string[] args)
        {
            RootCommand rootCommand = new RootCommand(
                description: "Monitors files and folders for changes.");

            Option<string> configFolder = new Option<string>(
                aliases: new string[] { "--folder", "-f" },
                description: "The folder containing the configuration and notification XML files.");
            rootCommand.AddOption(configFolder);

            Option<string> configName = new Option<string>(
                aliases: new string[] { "--configFile", "-cf" },
                description: "The name of the configuration XML file.");
            rootCommand.AddOption(configName);

            Option<string> notificationName = new Option<string>(
                aliases: new string[] { "--notificationFile", "-nf" },
                description: "The name of the notification XML file.");
            rootCommand.AddOption(notificationName);

            rootCommand.Handler = CommandHandler.Create<string, string, string>(RunWatcher);
            return rootCommand.Invoke(args);
        }

        /// <summary>
        /// Runs the file/folder watcher.
        /// </summary>
        /// <param name="folder">
        /// The folder where the config and notifications files are stored.
        /// </param>
        /// <param name="configFileName">
        /// The name of the configuration file.
        /// </param>
        /// <param name="notificationsFileName">
        /// The name of the notifications file.
        /// </param>
        /// <returns>
        /// Returns 0 if no error occurred, otherwise non-zero.
        /// </returns>
        private static int RunWatcher(string folder, string configFileName, string notificationsFileName)
        {
            Console.WriteLine($"File: {notificationsFileName}.");
            // Get the config file path
            string configFile = GetConfigFilePath(folder, configFileName);
            if (string.IsNullOrWhiteSpace(configFile))
            {
                return ERROR;
            }

            // Get the notifications file path
            string notificationsFile = GetNotificationsFilePath(folder, notificationsFileName);
            if (string.IsNullOrWhiteSpace(notificationsFile))
            {
                return ERROR;
            }

            // Load the watches information from the config XML file
            Watches watches = ReadConfigFile(configFile);
            if (watches == null)
            {
                return ERROR;
            }

            // Load the notifications from the XML file
            Notifications.Notifications notifications = ReadNotificationFile(notificationsFile);
            if (notifications == null)
            {
                return ERROR;
            }

            // Set the logger
            if (!SetLogger(watches))
            {
                return ERROR;
            }

            // Run the watcher tasks
            if (RunWatcherTasks(watches, notifications))
            {
                return SUCCESS;
            }
            else
            {
                return ERROR;
            }
        }

        /// <summary>
        /// Reads the configuration XML file.
        /// </summary>
        /// <param name="path">
        /// The path to the configuration XML file.
        /// </param>
        /// <returns>
        /// A watches object if the file was read successfully, otherwise null.
        /// </returns>
        private static Watches ReadConfigFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                Console.WriteLine("The configuration file path was null or empty.");
                return null;
            }

            if (!File.Exists(path))
            {
                Console.WriteLine($"The configuration file path '{path}' does not exist.");
                return null;
            }

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Watches));
                using FileStream fs = new FileStream(path, FileMode.Open);
                return (Watches)serializer.Deserialize(fs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"The configuration file could not be read. Reason: {ex.Message}");
                return null;
            }


        }

        /// <summary>
        /// Reads the notifications XML file.
        /// </summary>
        /// <param name="path">
        /// The path to the notifications XML file.
        /// </param>
        /// <returns>
        /// A Notifications object if the file was read successfully, otherwise null.
        /// </returns>
        private static Notifications.Notifications ReadNotificationFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                Console.WriteLine("The notifications file path was null or empty.");
                return null;
            }

            if (!File.Exists(path))
            {
                Console.WriteLine($"The notifications file path '{path}' does not exist.");
                return null;
            }

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Notifications.Notifications));
                using FileStream notifyfs = new FileStream(path, FileMode.Open);
                return (Notifications.Notifications)serializer.Deserialize(notifyfs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"The notification file could not be read. Reason: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Sets the logger.
        /// </summary>
        /// <param name="watches">
        /// The watches object that contains the log path.
        /// </param>
        /// <returns>
        /// True if the Logger was set, otherwise false.
        /// </returns>
        private static bool SetLogger(Watches watches)
        {
            if (watches == null)
            {
                Console.WriteLine("The watches object was not initialized.");
                return false;
            }

            try
            {
                Logger.SetFullPath(watches.Logging.LogPath);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"The log file could not be set. Reason: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Runs the watcher tasks as defined in the configuration XML file.
        /// </summary>
        /// <param name="watches">
        /// The watches.
        /// </param>
        /// <param name="notifications">
        /// The notifications.
        /// </param>
        /// <returns>
        /// True if the tasks were started and run successfully, otherwise false.
        /// </returns>
        private static bool RunWatcherTasks(Watches watches, Notifications.Notifications notifications)
        {
            if (watches == null)
            {
                Console.WriteLine("The watches object was not initialized.");
                return false;
            }

            if (notifications == null)
            {
                Console.WriteLine("The notifications object was not initialized.");
                return false;
            }

            Task[] tasks = new Task[watches.WatchList.Count];
            int count = 0;

            foreach (Watch watch in watches.WatchList)
            {
                try
                {
                    tasks[count] = Task.Run(() => { Watcher watcher = new Watcher(watch, notifications); });
                    count++;
                }
                catch (Exception ex)
                {
                    Logger.WriteLine(ex.Message, LogLevel.ERROR);
                }
            }

            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();

            try
            {
                Task.WaitAll(tasks);
            }
            catch (AggregateException ae)
            {
                foreach (Exception exception in ae.Flatten().InnerExceptions)
                {
                    Logger.WriteLine(exception.Message, LogLevel.ERROR);
                }
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the folder path containing the configuration and notification
        /// files.
        /// </summary>
        /// <param name="path">
        /// The folder path.
        /// </param>
        /// <returns>
        /// The folder path of the files, otherwise null.
        /// </returns>
        private static string GetFolderPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                try
                {
                    path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"The folder name is null or empty. Couldn't get the current location. Reason: {ex.Message}");
                    return null;
                }
            }

            if (Directory.Exists(path))
            {
                return path;
            }
            else
            {
                Console.WriteLine("The folder does not exist.");
                return null;
            }
        }

        /// <summary>
        /// Gets the full path to the configuration file.
        /// </summary>
        /// <param name="path">
        /// The path to the configuration file.
        /// </param>
        /// <param name="name">
        /// The name of the configuration file.
        /// </param>
        /// <returns>
        /// The full path to the configuration file, otherwise null.
        /// </returns>
        private static string GetConfigFilePath(string path, string name)
        {
            string folderPath = GetFolderPath(path);
            if (folderPath == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                name = DEFAULT_CONFIG_FILE;
            }

            try
            {
                string fullPath = Path.Combine(folderPath, name);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
                else
                {
                    Console.WriteLine($"The configuration file '{fullPath}' was not found.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not get the path to the configuration file. Reason: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the full path to the notifications file.
        /// </summary>
        /// <param name="path">
        /// The path to the notifications file.
        /// </param>
        /// <param name="name">
        /// The name of the notifications file.
        /// </param>
        /// <returns>
        /// The full path to the notifications file, otherwise null.
        /// </returns>
        private static string GetNotificationsFilePath(string path, string name)
        {
            string folderPath = GetFolderPath(path);
            if (folderPath == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                name = DEFAULT_NOTIFICATION_FILE;
            }

            try
            {
                string fullPath = Path.Combine(folderPath, name);
                if (File.Exists(fullPath))
                {
                    Console.WriteLine($"Notifications file: {fullPath}.");
                    return fullPath;
                }
                else
                {
                    Console.WriteLine($"The notifications file '{fullPath}' was not found.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not get the path to the notifications file. Reason: {ex.Message}");
                return null;
            }
        }
    }
}
