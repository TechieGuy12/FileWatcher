using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace TE.FileWatcher.Configuration
{
    /// <summary>
    /// The XML configuration file.
    /// </summary>
    public class XmlFile : IConfigurationFile
    {
        // The default configuration file name
        public const string DEFAULT_CONFIG_FILE = "config.xml";

        // Path to the configuration file
        private readonly string? _fullPath;

        /// <summary>
        /// Initializes an instance of the <see cref="XmlFile"/> class when
        /// provided with the path and name of the config file.
        /// </summary>
        /// <param name="path">
        /// The path to the config file.
        /// </param>
        /// <param name="name">
        /// The name of the config file.
        /// </param>
        /// <remarks>
        /// If the <paramref name="path"/> parameter is <c>null</c>, then the
        /// current folder path is used instead.
        /// If the <paramref name="name"/> parameter is <c>null</c>, then the
        /// value of <see cref="DEFAULT_CONFIG_FILE"/>.
        /// </remarks>
        public XmlFile(string path, string name)
        {
            _fullPath = GetFullPath(path, name);
        }

        /// <summary>
        /// Gets the folder path containing the configuration file.
        /// </summary>
        /// <param name="path">
        /// The folder path.
        /// </param>
        /// <returns>
        /// The folder path of the files, otherwise <c>null</c>.
        /// </returns>
        private static string? GetFolderPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                try
                {
                    path = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName);
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
        /// The full path to the configuration file, otherwise <c>null</c>.
        /// </returns>
        private static string? GetFullPath(string path, string name)
        {
            string? folderPath = GetFolderPath(path);
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
                    Console.WriteLine($"Configuration file: {fullPath}.");
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
        /// Reads the configuration file.
        /// </summary>
        /// <returns>
        /// A <see cref="Watches"/> object if the file was read successfully,
        /// otherwise <c>null</c>.
        /// </returns>
        [RequiresUnreferencedCode("Calls XmlSerializer")]
        public Watches? Read()
        {
            if (string.IsNullOrWhiteSpace(_fullPath))
            {
                Console.WriteLine("The configuration file path was null or empty.");
                return null;
            }

            if (!File.Exists(_fullPath))
            {
                Console.WriteLine($"The configuration file path '{_fullPath}' does not exist.");
                return null;
            }

            try
            {
                XmlSerializer serializer = new(typeof(Watches));
                using FileStream fs = new(_fullPath, FileMode.Open);
                return (Watches?)serializer.Deserialize(fs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"The configuration file could not be read. Reason: {ex.Message}");
                return null;
            }
        }
    }
}
