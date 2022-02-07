namespace TE.FileWatcher
{
    using System;

    public class FileWatcherException : Exception
    {
        public FileWatcherException()
        {
        }

        public FileWatcherException(string message)
            : base(message)
        {
        }

        public FileWatcherException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
