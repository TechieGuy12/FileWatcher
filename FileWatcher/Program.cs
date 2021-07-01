using System;
using System.IO;
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
            // Load the watches information from the config XML file
            Watches watches = ReadConfigFile(@"C:\Temp\config.xml");
            if (watches == null)
            {
                return ERROR;
            }

            // Load the notifications from the XML file
            Notifications.Notifications notifications = ReadNotificationFile(@"C:\Temp\notification.xml");
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
    }
}
