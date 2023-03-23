using DotNetIO = System.IO;
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

        // A megabyte
        private const int MEGABYTE = 1024 * 1024;

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
                using (var hashAlgorithm = SHA256.Create())
                {
                    using (var stream =
                        new FileStream(
                            fullPath,
                            FileMode.Open,
                            FileAccess.Read,
                            FileShare.None,
                            MEGABYTE))
                    {
                        var hash = hashAlgorithm.ComputeHash(stream);
                        return BitConverter.ToString(hash).Replace("-", "", StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            catch (Exception ex)
                when (ex is ArgumentException || ex is ArgumentNullException || ex is ObjectDisposedException || ex is System.Reflection.TargetInvocationException)
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

            return (sourceHash.Equals(destinationHash, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Waits for a file to be accessible.
        /// </summary>
        /// <param name="path">
        /// Path to the file.
        /// </param>
        /// <exception cref="ArgumentException">
        /// <c>path</c> is a zero-length string, contains only white space, or
        /// contains one or more invalid characters.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <c>path</c> is null.
        /// </exception>
        /// <exception cref="PathTooLongException">
        /// The specified path, file name, or both exceed the system-defined
        /// maximum length.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        /// The specified path is invalid, (for example, it is on an unmapped
        /// drive).
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// <c>path</c> specified a directory, or, the caller does not have the
        /// required permission.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// The file specified in <c>path</c> was not found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <c>path</c> is an invalid format.
        /// </exception>
        private static void WaitForFile(string path)
        {            
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }
            
            if (!DotNetIO.File.Exists(path))
            {
                throw new FileNotFoundException(
                    $"The file '{path}' was not found.", path);
            }
            
            while (true)                
            try
            {                
                using FileStream fileStream = DotNetIO.File.OpenRead(path);
                    break;
            }
            catch (Exception ex)
                when (ex is FileNotFoundException || ex is DirectoryNotFoundException || ex is PathTooLongException || ex is NotSupportedException || ex is UnauthorizedAccessException)
            {
                throw;
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

            if (!DotNetIO.File.Exists(source))
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
                    DotNetIO.File.Copy(source, destination, true);
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
            if (string.IsNullOrWhiteSpace(path) || !DotNetIO.File.Exists(path))
            {
                return null;
            }

            try
            {
                return DotNetIO.File.GetCreationTime(path);
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
            if (string.IsNullOrWhiteSpace(path) || !DotNetIO.File.Exists(path))
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
            if (string.IsNullOrWhiteSpace(path) || !DotNetIO.File.Exists(path))
            {
                return null;
            }

            try
            {
                return DotNetIO.File.GetLastWriteTime(path);
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
            if (string.IsNullOrWhiteSpace(path) || !DotNetIO.File.Exists(path))
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
        /// <exception cref="FileWatcherException">
        /// Thrown when the file could not be validated.
        /// </exception>
        public static bool IsValid(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            if (DotNetIO.File.Exists(path))
            {
                try
                {
                    WaitForFile(path);
                    return true;
                }
                catch (Exception ex)
                {
                    throw new FileWatcherException($"The file '{path}' is not valid. Reason: {ex.Message}");
                }
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

            if (!DotNetIO.File.Exists(source))
            {
                return;
            }

            try
            {
                int attempts = 0;
                bool fileDeleted = false;
                while ((attempts <= RETRIES) && !fileDeleted)
                {
                    DotNetIO.File.Delete(source);
                    fileDeleted = !DotNetIO.File.Exists(source);

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
                DotNetIO.File.SetCreationTime(destination, (DateTime)sourceTime);
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
                DotNetIO.File.SetLastWriteTime(destination, (DateTime)sourceTime);
            }
            catch
            {
                // Just swallow the exception as we are just setting the time
                return;
            }
        }
    }
}
