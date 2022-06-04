using IO = System.IO;
using System.Security.Cryptography;

namespace TE.FileWatcher.FileSystem
{
    /// <summary>
    /// A wrapper class to manage the File actions.
    /// </summary>
    public static class File
    {
        // The number of times to retry a file action
        private const int RETRIES = 5;

        /// <summary>
        /// Gets the hash of the file.
        /// </summary>
        /// <param name="fullPath">
        /// The full path to the file.
        /// </param>
        /// <returns>
        /// The hash of the file, otherwise <c>null</c>.
        /// </returns>
        private static string? GetFileHash(string fullPath)
        {
            try
            {
                using var hashAlgorithm = SHA256.Create();
                if (hashAlgorithm == null)
                {
                    return null;
                }

                using var stream = IO.File.OpenRead(fullPath);
                var hash = hashAlgorithm.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "");
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Verifies the files and returns a value indicating whether the files
        /// are the same.
        /// </summary>
        /// <param name="source">
        /// The full path to the source file.
        /// </param>
        /// <param name="destination">
        /// The full path to the destination file.
        /// </param>
        /// <returns>
        /// <c>true</c> if the verification was successful, otherwise <c>false</c>.
        /// </returns>
        private static bool Verify(string source, string destination)
        {
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(destination))
            {
                return false;
            }

            string? sourceHash = GetFileHash(source);
            string? destinationHash = GetFileHash(destination);
            
            if (string.IsNullOrWhiteSpace(sourceHash))
            {
                return false;
            }

            return (sourceHash.Equals(destinationHash));
        }

        /// <summary>
        /// Waits for a file to be accessible.
        /// </summary>
        /// <param name="path">
        /// Path to the file.
        /// </param>
        private static void WaitForFile(string path)
        {
            while (true)
            try
            {
                using FileStream fileStream = IO.File.OpenRead(path);
                    break;
            }
            catch
            {
                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// Copies a file to a specified location.
        /// </summary>
        /// <param name="source">
        /// The source file to copy.
        /// </param>
        /// <param name="destination">
        /// The copy destination location.
        /// </param>
        /// <param name="verify">
        /// Verify the file after the copy has completed.
        /// </param>
        /// <returns>
        /// <c>true</c> if the file was copied successfully, otherwise <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when one of the parameters are <c>null</c>.
        /// </exception>
        /// <exception cref="IO.FileNotFoundException">
        /// Thrown when the file specified by the <paramref name="source"/> is not found.
        /// </exception>
        /// <exception cref="FileWatcherException">
        /// Thrown when the file could not be copied to the destination.
        /// </exception>
        public static void Copy(string source, string destination, bool verify, bool keepTimestamp)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (string.IsNullOrWhiteSpace(destination))
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (!IO.File.Exists(source))
            {
                return;
            }

            if (Directory.IsValid(source))
            {
                return;
            }
          
            try
            {
                WaitForFile(source);
                Directory.Create(destination);

                int attempts = 0;
                bool fileCopied = false;
                while ((attempts <= RETRIES) && !fileCopied)
                {                    
                    IO.File.Copy(source, destination, true);
                    WaitForFile(destination);

                    fileCopied = verify != true || Verify(source, destination);                  

                    if (!fileCopied)
                    {
                        attempts++;
                    }
                }

                if (keepTimestamp)
                {
                    // Set the time of the destination file to match the source file
                    // because the file was moved and not a new copy
                    SetDestinationCreationTime(source, destination);
                    SetDestinationModifiedTime(source, destination);
                }
            }
            catch (Exception ex)
            {
                throw new FileWatcherException("The file could not be copied.", ex);
            }            
        }

        /// <summary>
        /// Gets the creation date/time for the file.
        /// </summary>
        /// <param name="path">
        /// The full path to the file.
        /// </param>
        /// <returns>
        /// The creation date/time of the file, otherwise <c>null</c>.
        /// </returns>
        /// <exception cref="FileWatcherException">
        /// Thrown when the creation time could not be retrieved from the file.
        /// </exception>
        public static DateTime? GetCreatedDate(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !IO.File.Exists(path))
            {
                return null;
            }

            try
            {
                return IO.File.GetCreationTime(path);
            }
            catch (Exception ex)
            {
                throw new FileWatcherException($"The creation time could not be retrieved from the file '{path}'. Reason: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the file extension.
        /// </summary>
        /// <param name="path">
        /// The full path to the file.
        /// </param>
        /// <returns>
        /// The extension of the full, otherwise <c>null</c>.
        /// </returns>
        public static string? GetExtension(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !IO.File.Exists(path))
            {
                return null;
            }

            return Path.GetExtension(path);
        }

        /// <summary>
        /// Gets the modified date/time for the file.
        /// </summary>
        /// <param name="path">
        /// The full path to the file.
        /// </param>
        /// <returns>
        /// The modified date/time of the file, otherwise <c>null</c>.
        /// </returns>
        /// <exception cref="FileWatcherException">
        /// Thrown when the modified time could not be retrieved from the file.
        /// </exception>
        public static DateTime? GetModifiedDate(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !IO.File.Exists(path))
            {
                return null;
            }

            try
            {
                return IO.File.GetLastWriteTime(path);
            }
            catch (Exception ex)
            {
                throw new FileWatcherException($"The modified time could not be retrieved from the file '{path}'. Reason: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the name of the file with or without the extension.
        /// </summary>
        /// <param name="path">
        /// The full path to the file.
        /// </param>
        /// <returns>
        /// The name of the file, otherwise <c>null</c>.
        /// </returns>
        public static string? GetName(string path, bool includeExtension)
        {
            if (string.IsNullOrWhiteSpace(path) || !IO.File.Exists(path))
            {
                return null;
            }

            return includeExtension ? Path.GetFileNameWithoutExtension(path) : Path.GetFileName(path);
        }

        /// <summary>
        /// Returns a flag indicating the file is valid.
        /// </summary>
        /// <param name="path">
        /// The path to the file.
        /// </param>
        /// <returns>
        /// <c>true</c> if the file is valid, otherwise <c>false</c>.
        /// </returns>
        public static bool IsValid(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            if (IO.File.Exists(path))
            {
                WaitForFile(path);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Moves a file to a specified location.
        /// </summary>
        /// <param name="source">
        /// The source file to move.
        /// </param>
        /// <param name="destination">
        /// The move destination location.
        /// </param>
        /// <param name="verify">
        /// Verify the file after the copy has completed.
        /// </param>
        /// <param name="keepTimestamp">
        /// Flag indicating the created and modified timestamps of the source
        /// file will be applied to the destination file.
        /// </param>
        /// <returns>
        /// <c>true</c> if the file was moved successfully, otherwise <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when one of the parameters are <c>null</c>.
        /// </exception>
        /// <exception cref="IO.FileNotFoundException">
        /// Thrown when the file specified by the <paramref name="source"/> is not found.
        /// </exception>
        /// <exception cref="FileWatcherException">
        /// Thrown when the file could not be moved to the destination.
        /// </exception>
        public static void Move(string source, string destination, bool verify, bool keepTimestamp)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (string.IsNullOrWhiteSpace(destination))
            {
                throw new ArgumentNullException(nameof(destination));
            }

            Copy(source, destination, verify, keepTimestamp);
            Delete(source);
        }

        /// <summary>
        /// Deletes a file.
        /// </summary>
        /// <param name="source">
        /// The full path of the file to delete.
        /// </param>
        /// <returns>
        /// <c>true</c> the file was deleted successfully, otherwise <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when one of the parameters are <c>null</c>.
        /// </exception>
        /// <exception cref="FileWatcherException">
        /// Thrown when the file could not be deleted.
        /// </exception>
        public static void Delete(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (!IO.File.Exists(source))
            {
                return;
            }

            try
            {
                int attempts = 0;
                bool fileDeleted = false;
                while ((attempts <= RETRIES) && !fileDeleted)
                {
                    IO.File.Delete(source);
                    fileDeleted = !IO.File.Exists(source);

                    if (!fileDeleted)
                    {
                        attempts++;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new FileWatcherException("The file could not be deleted.", ex);
            }
        }

        /// <summary>
        /// Set the creation time on the destionation file to match the source
        /// file.
        /// </summary>
        /// <param name="source">
        /// Full path to the source file.
        /// </param>
        /// <param name="destination">
        /// Full path to the destination file.
        /// </param>
        private static void SetDestinationCreationTime(string source, string destination)
        { 
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(destination))
            {
                return;
            }

            if (!IsValid(source) || !IsValid(destination))
            {
                return;
            }

            DateTime? sourceTime = GetCreatedDate(source);
            if (sourceTime == null)
            {
                return;
            }

            try
            {
                IO.File.SetCreationTime(destination, (DateTime)sourceTime);
            }
            catch
            {
                // Just swallow the exception as we are just setting the time
                return;
            }
        }

        /// <summary>
        /// Set the modified time on the destionation file to match the source
        /// file.
        /// </summary>
        /// <param name="source">
        /// Full path to the source file.
        /// </param>
        /// <param name="destination">
        /// Full path to the destination file.
        /// </param>
        private static void SetDestinationModifiedTime(string source, string destination)
        {
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(destination))
            {
                return;
            }

            if (!IsValid(source) || !IsValid(destination))
            {
                return;
            }

            DateTime? sourceTime = GetModifiedDate(source);
            if (sourceTime == null)
            {
                return;
            }

            try
            {
                IO.File.SetLastWriteTime(destination, (DateTime)sourceTime);
            }
            catch
            {
                // Just swallow the exception as we are just setting the time
                return;
            }
        }
    }
}
