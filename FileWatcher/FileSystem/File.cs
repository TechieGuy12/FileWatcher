using System;
using System.Collections.Generic;
using IO = System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;
using System.Threading;

namespace TE.FileWatcher.FileSystem
{
    /// <summary>
    /// A wrapper class to manage the File actions.
    /// </summary>
    public static class File
    {
        // The number of times to retry a file action
        private const int RETRIES = 5;

        // The hash algorithm to use when verifying the files
        private const string HASH_ALGORITHM = "SHA256";

        /// <summary>
        /// Gets the hash of the file.
        /// </summary>
        /// <param name="fullPath">
        /// The full path to the file.
        /// </param>
        /// <returns>
        /// The hash of the file, otherwise <c>null</c>.
        /// </returns>
        private static string GetFileHash(string fullPath)
        {
            try
            {
                using (var hashAlgorithm = HashAlgorithm.Create(HASH_ALGORITHM))
                {
                    using (var stream = IO.File.OpenRead(fullPath))
                    {
                        var hash = hashAlgorithm.ComputeHash(stream);
                        return BitConverter.ToString(hash).Replace("-", "");
                    }
                }
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
            string sourceHash = GetFileHash(source);
            string destinationHash = GetFileHash(destination);

            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(destination))
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
        public static void Copy(string source, string destination, bool verify)
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
                //throw new FileNotFoundException($"The file '{source}' was not found.");
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

                    fileCopied = (verify == true) ? Verify(source, destination) : true;
                    fileCopied = true;                    

                    if (!fileCopied)
                    {
                        attempts++;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new FileWatcherException("The file could not be copied.", ex);
            }            
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
        public static void Move(string source, string destination, bool verify)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (string.IsNullOrWhiteSpace(destination))
            {
                throw new ArgumentNullException(nameof(destination));
            }

            Copy(source, destination, verify);
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
    }
}
