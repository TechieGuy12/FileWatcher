using DotNetIO = System.IO;

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
		/// <exception cref="ArgumentNullException">
		/// Thrown when an argument is null or empty.
		/// </exception>
		/// <exception cref="FileWatcherException">
		/// Thrown when the directory could not be created.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown when the directory from the path is null.
		/// </exception>
		public static void Create(string path)
        {
			if (string.IsNullOrWhiteSpace(path))
            {
				throw new ArgumentNullException(nameof(path));
            }

			string? folders = Path.GetDirectoryName(path);

			if (!string.IsNullOrWhiteSpace(folders))
			{
				// If the destination directory doesn't exist, create
				// create it to avoid any exceptions						
				if (!DotNetIO.Directory.Exists(folders))
				{
					DotNetIO.Directory.CreateDirectory(folders);
					if (!DotNetIO.Directory.Exists(folders))
                    {
						throw new FileWatcherException($"The directory {folders} could not be created.");
                    }
				}
			}
			else
            {
				throw new InvalidOperationException("The directory path could not be determined.");
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

			return DotNetIO.Directory.Exists(path);
        }
    }
}
