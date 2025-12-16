namespace TE.FileWatcher.Log
{
    /// <summary>
    /// The message to write to the log.
    /// </summary>
    public class Message
    {
        /// <summary>
        /// Gets or sets the value of the message.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the level of the message.
        /// </summary>
        public LogLevel Level { get; set; } = LogLevel.INFO;

        /// <summary>
        /// Gets the timestamp when the message was created (high precision).
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Gets the high-precision ticks for sub-millisecond accuracy.
        /// </summary>
        public long Ticks { get; }

        /// <summary>
        /// Gets the string representation of the log level.
        /// </summary>
        public string LevelString
        {
            get
            {
                return Level switch
                {
                    LogLevel.DEBUG => "DEBUG",
                    LogLevel.WARNING => "WARN ",
                    LogLevel.ERROR => "ERROR",
                    LogLevel.FATAL => "FATAL",
                    _ => "INFO ",
                };
            }
        }

        /// <summary>
        /// Gets the formatted timestamp with microsecond precision.
        /// Format: yyyy-MM-dd HH:mm:ss.ffffff
        /// </summary>
        public string FormattedTimestamp
        {
            get
            {
                // Calculate microseconds from ticks (10 ticks = 1 microsecond)
                long microseconds = (Ticks % TimeSpan.TicksPerMillisecond) / 10;
                return $"{Timestamp:yyyy-MM-dd HH:mm:ss.fff}{microseconds:D3}";
            }
        }

        /// <summary>
        /// Initializes a <see cref="Message"/> class when provided with the
        /// message value and log level.
        /// </summary>
        /// <param name="value">
        /// The message value.
        /// </param>
        /// <param name="level">
        /// The level of the message.
        /// </param>
        public Message(string value, LogLevel level)
        {
            Value = value;
            Level = level;
            Timestamp = DateTime.Now;
            Ticks = Timestamp.Ticks;
        }
    }
}
