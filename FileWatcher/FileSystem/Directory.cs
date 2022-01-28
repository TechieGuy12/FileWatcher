using System;
using System.Collections.Generic;
using IO = System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TE.FileWatcher.FileSystem
{
	/// <summary>
	/// Contains methods to work with directories.
	/// </summary>
    public static class Directory
    {
		/// <summary>
		/// Creates the folder structure specified by a path.
		/// </summary>
		/// <param name="path">
		/// The path that includes the folder structure to create.
		/// </param>
        public static void Create(string path)
        {
			if (string.IsNullOrWhiteSpace(path))
            {
				return;
            }

			string? folders = Path.GetDirectoryName(path);

			if (!string.IsNullOrWhiteSpace(folders))
			{
				// If the destination directory doesn't exist, create
				// create it to avoid any exceptions						
				if (!IO.Directory.Exists(folders))
				{
					IO.Directory.CreateDirectory(folders);
				}
			}
		}

		/// <summary>
		/// Returns a value indicating the path provided is a valid directory.
		/// </summary>
		/// <param name="path">
		/// The path of the directory.
		/// </param>
		/// <returns>
		/// <c>true</c> if the path is a valid directory, otherwise <c>false</c>.
		/// </returns>
		public static bool IsValid(string path)
        {
			if (string.IsNullOrWhiteSpace(path))
            {
				return false;
            }

			return IO.Directory.Exists(path);
        }
    }
}
