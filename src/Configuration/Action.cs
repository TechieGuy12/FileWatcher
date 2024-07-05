using System.Xml.Serialization;
using TE.FileWatcher.Log;
using TEFS = TE.FileWatcher.FileSystem;

namespace TE.FileWatcher.Configuration
{
    /// <summary>
    /// The Action to perform during a watch event.
    /// </summary>
    public class Action : RunnableBase
    {
        /// <summary>
        /// The type of action to perform.
        /// </summary>
        [Serializable]
        public enum ActionType
        {
            /// <summary>
            /// Copy a file.
            /// </summary>
            Copy,
            /// <summary>
            /// Move a file.
            /// </summary>
            Move,
            /// <summary>
            /// Delete a file.
            /// </summary>
            Delete
        }

        /// <summary>
        /// Gets or sets the type of action to perform.
        /// </summary>
        [XmlElement("type")]
        public ActionType Type { get; set; }

        /// <summary>
        /// Gets or sets the source of the action.
        /// </summary>
        [XmlElement("source")]
        public string Source { get; set; } = Placeholder.PLACEHOLDERFULLPATH;

        /// <summary>
        /// Gets or sets the destination of the action.
        /// </summary>
        [XmlElement("destination")]
        public string? Destination { get; set; }

        /// <summary>
        /// Gets or sets the verify flag.
        /// </summary>
        [XmlElement(ElementName = "verify", DataType = "boolean")]
        public bool Verify { get; set; }

        /// <summary>
        /// Gets or sets the keep timestamps flag.
        /// </summary>
        [XmlElement(ElementName = "keepTimestamps", DataType = "boolean")]
        public bool KeepTimestamps { get; set; }

        /// <summary>
        /// Runs the action.
        /// </summary>
        /// <param name="watchPath">
        /// The watch path.
        /// </param>
        /// <param name="fullPath">
        /// The full path to the changed file or folder.
        /// </param>
        /// <param name="trigger">
        /// The trigger for the action.
        /// </param>
        public override void Run(ChangeInfo change, TriggerType trigger)
        {
            try
            {
                base.Run(change, trigger);
            }
            catch (ArgumentNullException e)
            {
                Logger.WriteLine(e.Message);
                return;
            }
            catch (InvalidOperationException e)
            {
                Logger.WriteLine(e.Message);
                return;
            }
            catch (FileWatcherTriggerNotMatchException)
            {
                return;
            }

            Logger.WriteLine($"Waiting for {WaitBefore} milliseconds. (Action.Run)", LogLevel.DEBUG);
            Thread.Sleep(WaitBefore);

            string? source = GetSource();
            string? destination = GetDestination();

            if (string.IsNullOrWhiteSpace(source))
            {
                if (Change != null)
                {
                    Logger.WriteLine(
                        $"The source file could not be determined. Watch path: {Change.WatchPath}, changed: {Change.FullPath}.",
                        LogLevel.ERROR);
                }
                return;
            }

            try
            {
                if (!TEFS.File.IsValid(source))
                {
                    Logger.WriteLine(
                        $"The file '{source}' could not be {GetActionString()} because the path was not valid, the file doesn't exists, or it was in use.",
                        LogLevel.ERROR);
                    return;
                }

                switch (Type)
                {
                    case ActionType.Copy:
                        if (string.IsNullOrWhiteSpace(destination))
                        {
                            Logger.WriteLine(
                                $"The file '{source}' could not be copied because the destination file could not be determined. Destination in config file: {Destination}.",
                                LogLevel.ERROR);
                            return;
                        }

                        TEFS.File.Copy(source, destination, Verify, KeepTimestamps);
                        Logger.WriteLine($"Copied {source} to {destination}. Verify: {Verify}. Keep timestamps: {KeepTimestamps}.");
                        break;

                    case ActionType.Move:
                        if (string.IsNullOrWhiteSpace(destination))
                        {
                            Logger.WriteLine(
                                $"The file '{source}' could not be moved because the destination file could not be determined. Destination in config file: {Destination}.",
                                LogLevel.ERROR);
                            return;
                        }

                        TEFS.File.Move(source, destination, Verify, KeepTimestamps);
                        Logger.WriteLine($"Moved {source} to {destination}. Verify: {Verify}. Keep timestamps: {KeepTimestamps}.");
                        break;

                    case ActionType.Delete:
                        TEFS.File.Delete(source);
                        Logger.WriteLine($"Deleted {source}.");
                        break;
                }
            }
            catch (Exception ex)
                when (ex is ArgumentNullException || ex is FileNotFoundException || ex is FileWatcherException)
            {
                Exception exception = ex.InnerException ?? ex;
                Logger.WriteLine(
                    $"Could not {Type.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture)} file '{source}.' Reason: {exception.Message}",
                    LogLevel.ERROR);
                if (ex.StackTrace != null)
                {
                    Logger.WriteLine(ex.StackTrace);
                }
                return;
            }
        }

        /// <summary>
        /// Gets the string value that represents the action type.
        /// </summary>
        /// <returns>
        /// A string value for the action type, otherwise <c>null</c>.
        /// </returns>
        private string? GetActionString()
        {
            return Type switch
            {
                ActionType.Copy => "copied",
                ActionType.Move => "moved",
                ActionType.Delete => "deleted",
                _ => null
            };
        }

        /// <summary>
        /// Gets the destination value by replacing any placeholders with the
        /// actual string values.
        /// </summary>
        /// <returns>
        /// The destination string value.
        /// </returns>
        private string? GetDestination()
        {
            if (string.IsNullOrWhiteSpace(Destination) || Change == null)
            {
                return null;
            }

            return Placeholder.ReplacePlaceholders(
                Destination,
                Change.WatchPath,
                Change.FullPath,
                Change.OldPath,
                Variables?.AllVariables);
        }

        /// <summary>
        /// Gets the source value by replacing any placeholders with the actual
        /// string values.
        /// </summary>
        /// <returns>
        /// The source string value.
        /// </returns>
        private string? GetSource()
        {
            if (string.IsNullOrWhiteSpace(Source) || Change == null)
            {
                return null;
            }

            return Placeholder.ReplacePlaceholders(
                Source,
                Change.WatchPath,
                Change.FullPath,
                Change.OldPath,
                Variables?.AllVariables);
        }
    }
}
