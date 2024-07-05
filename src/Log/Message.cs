﻿namespace TE.FileWatcher.Log
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
        }
    }
}
