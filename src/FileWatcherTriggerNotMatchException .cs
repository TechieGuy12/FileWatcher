namespace TE.FileWatcher
{
    using System;

    public class FileWatcherTriggerNotMatchException : Exception
    {
        public FileWatcherTriggerNotMatchException()
        {
        }

        public FileWatcherTriggerNotMatchException(string message)
            : base(message)
        {
        }

        public FileWatcherTriggerNotMatchException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
