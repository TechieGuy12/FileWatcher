using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public Watches Read();
    }
}
