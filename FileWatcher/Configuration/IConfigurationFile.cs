using System.Diagnostics.CodeAnalysis;

namespace TE.FileWatcher.Configuration
{
    interface IConfigurationFile
    {
        /// <summary>
        /// Reads the configuration file.
        /// </summary>
        /// <returns>
        /// A <see cref="Watches"/> object if the file was read successfully,
        /// otherwise null.
        /// </returns>
        [RequiresUnreferencedCode("Could call functionality incompatible with trimming.")]
        public Watches? Read();
    }
}
