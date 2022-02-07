namespace TE.FileWatcher.Configuration
{
    public class ChangeInfo
    {
        /// <summary>
        /// Gets the trigger for the change.
        /// </summary>
        public TriggerType Trigger { get; private set; }

        /// <summary>
        /// Gets the name of the file/folder.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the full path to the file/folder.
        /// </summary>
        public string FullPath { get; private set; }

        /// <summary>
        /// Initializes an instance of the <see cref="ChangeInfo"/>.
        /// </summary>
        //public ChangeInfo() { }

        /// <summary>
        /// Initializes an instance of the <see cref="ChangeInfo"/> class when
        /// provided with the trigger, the file/folder name, and the full path
        /// to the file/folder.
        /// </summary>
        /// <param name="trigger">
        /// The type of change.
        /// </param>
        /// <param name="name">
        /// The name of the file or folder.
        /// </param>
        /// <param name="fullPath">
        /// The full path of the file or folder.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a parameter is null.
        /// </exception>
        public ChangeInfo(TriggerType trigger, string name, string fullPath)
        {
            Trigger = trigger;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            FullPath = fullPath ?? throw new ArgumentNullException(nameof(fullPath));
        }
    }
}
