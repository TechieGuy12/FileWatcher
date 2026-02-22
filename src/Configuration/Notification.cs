using System.Globalization;
using System.Text;
using System.Xml.Serialization;
using TE.FileWatcher.Log;
using TE.FileWatcher.Net;

namespace TE.FileWatcher.Configuration
{
    /// <summary>
    /// A notification that will be triggered.
    /// </summary>
    public class Notification : RunnableBase
    {
        // The message to send with the request
        private readonly StringBuilder _message;

        /// <summary>
        /// Gets or sets the URL of the request.
        /// </summary>
        [XmlElement("url")]
        public string? Url { get; set; }

        /// <summary>
        /// Gets or sets the string representation of the request method.
        /// </summary>
        [XmlElement("method")]
        public string? MethodString { get; set; }

        /// <summary>
        /// Gets the request method.
        /// </summary>
        [XmlIgnore]
        public HttpMethod Method
        {
            get
            {
                if (string.IsNullOrWhiteSpace(MethodString))
                {
                    return HttpMethod.Post;
                }

                return MethodString.ToLower(CultureInfo.CurrentCulture) switch
                {
                    "get" => HttpMethod.Get,
                    "delete" => HttpMethod.Delete,
                    "put" => HttpMethod.Put,
                    _ => HttpMethod.Post,
                };
            }
        }

        /// <summary>
        /// Gets or sets the data to send for the request.
        /// </summary>
        [XmlElement("data")]
        public Data? Data { get; set; }

        /// <summary>
        /// Returns a value indicating if there is a message waiting to be sent
        /// for the notification.
        /// </summary>
        [XmlIgnore]
        public bool HasMessage
        {
            get
            {
                if (_message == null)
                {
                    return false;
                }

                return _message.Length > 0;
            }
        }

        /// <summary>
        /// Initializes an instance of the <see cref="Notification"/>class.
        /// </summary>
        public Notification()
        {
            _message = new StringBuilder();
        }

        /// <summary>
        /// Gets the string value for the message type.
        /// </summary>
        /// <param name="trigger">
        /// The notification trigger.
        /// </param>
        /// <returns>
        /// The string value for the message type, otherwise <c>null</c>.
        /// </returns>
        private static string GetMessageString(ChangeInfo change)
        {
            string? messageType = null;
            switch (change.Trigger)
            {
                case TriggerType.Create:
                    messageType = "Created";
                    break;
                case TriggerType.Change:
                    messageType = "Changed";
                    break;
                case TriggerType.Delete:
                    messageType = "Deleted";
                    break;
                case TriggerType.Rename:
                    messageType = "Renamed";
                    break;
            }

            return CleanMessage($"{messageType}: {change.FullPath}\n");
        }

        /// <summary>
        /// Sends the notification.
        /// </summary>
        /// <param name="message">
        /// The value that replaces the <c>[message]</c> placeholder.
        /// </param>
        /// <param name="trigger">
        /// The trigger for the request.
        /// </param>
        /// <param name="watchPath">
        /// The watch path.
        /// </param>
        /// <param name="change">
        /// Information about the change.
        /// </param>
        internal void QueueRequest(TriggerType trigger, ChangeInfo change)
        {
            if (Triggers == null || Triggers.TriggerList == null || Triggers.TriggerList.Count <= 0)
            {
                if (Logger.LogLevel <= LogLevel.DEBUG)
                {
                    Logger.WriteLine($"[{change.CorrelationId}] Notification.QueueRequest: No triggers configured, skipping.", LogLevel.DEBUG);
                }
                return;
            }

            if (Triggers.Current.HasFlag(trigger))
            {                
                Change = change;
                _message.Append(GetMessageString(Change));
                
                if (Logger.LogLevel <= LogLevel.DEBUG)
                {
                    Logger.WriteLine($"[{change.CorrelationId}] Notification.QueueRequest: Queued message for trigger {trigger}. Message length: {_message.Length}.", LogLevel.DEBUG);
                }
            }
            else
            {
                if (Logger.LogLevel <= LogLevel.DEBUG)
                {
                    Logger.WriteLine($"[{change.CorrelationId}] Notification.QueueRequest: Trigger {trigger} not in configured triggers ({Triggers.Current}), skipping.", LogLevel.DEBUG);
                }
            }
        }

        /// <summary>
        /// Send the notification request.
        /// </summary>
        /// <param name="change">
        /// Information about the file change to use for this notification. If null, uses the stored Change property.
        /// </param>
        /// <param name="message">
        /// Optional message to send. If null, uses the accumulated _message buffer.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the URL is null or empty.
        /// </exception>
        /// <exception cref="UriFormatException">
        /// Thrown when the URL is not in a valid format.
        /// </exception>
        internal async Task<Response?> SendAsync(ChangeInfo? change = null, string? message = null)
        {
            // Determine which message to use
            string? messageToSend = message;
            if (string.IsNullOrEmpty(messageToSend))
            {
                // Fall back to the accumulated _message buffer (for queued notifications)
                if (_message == null || _message.Length <= 0)
                {
                    return null;
                }
                messageToSend = _message.ToString();
            }

            // Use provided change or fall back to stored Change property
            ChangeInfo? changeToUse = change ?? Change;
            if (changeToUse == null)
            {
                throw new InvalidOperationException("The change information cannot be null.");
            }

            // CRITICAL: Validate correlation ID matches if both are provided
            if (change != null && Change != null && change.CorrelationId != Change.CorrelationId)
            {
                Logger.WriteLine(
                    $"[{change.CorrelationId}] CRITICAL: Correlation ID mismatch detected! " +
                    $"Parameter: [{change.CorrelationId}] ({change.FullPath}), " +
                    $"Stored: [{Change.CorrelationId}] ({Change.FullPath}). " +
                    $"This indicates instance reuse issue! (Notification.SendAsync)",
                    LogLevel.ERROR);
            }

            Uri uri = GetUri(changeToUse);
            
            Data ??= new Data();
            if (Data.Headers != null)
            {
                Data.Headers.Variables ??= new Variables();
                Data.Headers.Variables.Add(Variables?.AllVariables);
                Data.Headers.Change = changeToUse;
            }

            string? content = string.Empty;
            if (Data.Body != null)
            {
                content = Data.Body.Replace("[message]", messageToSend, StringComparison.OrdinalIgnoreCase);

                if (changeToUse != null)
                {
                    content = Placeholder.ReplacePlaceholders(
                        content,
                        changeToUse.WatchPath,
                        changeToUse.FullPath,
                        changeToUse.OldPath,
                        Variables?.AllVariables);
                }
            }

            string correlationIdLog = changeToUse != null ? $"[{changeToUse.CorrelationId}] " : "";
            Logger.WriteLine($"{correlationIdLog}Sending request: {Method} {uri}.");
            Logger.WriteLine($"{correlationIdLog}Message content: {content}", LogLevel.DEBUG);
            
            Response response =
                await Request.SendAsync(
                    Method,
                    uri,
                    Data.Headers,
                    content,
                    Data.MimeType).ConfigureAwait(false);

            // Only clear the shared _message buffer if we used it (queued notification path)
            if (string.IsNullOrEmpty(message))
            {
                _message.Clear();
            }
            
            return response;
        }

        /// <summary>
        /// Escapes the special characters in the message so it can be sent as
        /// a JSON string.
        /// </summary>
        /// <param name="s">
        /// The message to escape.
        /// </param>
        /// <returns>
        /// The JSON string with the special characters escaped.
        /// </returns>
        private static string CleanMessage(string s)
        {
            if (s == null || s.Length == 0)
            {
                return "";
            }

            char c = '\0';
            int i;
            int len = s.Length;
            StringBuilder sb = new(len + 4);
            string t;

            for (i = 0; i < len; i += 1)
            {
                c = s[i];
                switch (c)
                {
                    case '\\':
                    case '"':
                        sb.Append('\\');
                        sb.Append(c);
                        break;
                    case '/':
                        sb.Append('\\');
                        sb.Append(c);
                        break;
                    case '\b':
                        sb.Append("\\b");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\f':
                        sb.Append("\\f");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    default:
                        if (c < ' ')
                        {
                            t = "000" + string.Format(CultureInfo.CurrentCulture,"{0:X}", c);
                            sb.Append(string.Concat("\\u", t.AsSpan(t.Length - 4)));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Gets the URI value of the string URL.
        /// </summary>
        /// <param name="change">
        /// Information about the file change to use for building the URI.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when either the watch path or the change information was not provided.
        /// </exception>
        /// <exception cref="UriFormatException">
        /// Thrown if the URL is not in a valid format.
        /// </exception>
        private Uri GetUri(ChangeInfo change)
        {
            if (change == null)
            {
                throw new InvalidOperationException("The change information cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(Url))
            {
                throw new UriFormatException();
            }

            string? url = Placeholder.ReplacePlaceholders(
                Url,
                change.WatchPath,
                change.FullPath,
                change.OldPath,
                Variables?.AllVariables);

            if (string.IsNullOrWhiteSpace(url))
            {
                throw new UriFormatException($"The notification URL: {url} is not valid.");
            }
            Logger.WriteLine($"URL: {url}.");
            Uri uri = new(url);
            return uri;
        }

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
            // Log entry with correlation ID for traceability
            Logger.WriteLine(
                $"[{change.CorrelationId}] Notification.Run() called for file: {change.FullPath} (Notification.Run)",
                LogLevel.DEBUG);
            
            try
            {
                base.Run(change, trigger);
            }
            catch (ArgumentNullException e)
            {
                Logger.WriteLine($"[{change.CorrelationId}] {e.Message}", LogLevel.ERROR);
                return;
            }
            catch (InvalidOperationException e)
            {
                Logger.WriteLine($"[{change.CorrelationId}] {e.Message}", LogLevel.ERROR);
                return;
            }
            catch (FileWatcherTriggerNotMatchException)
            {
                return;
            }

            // Store change in the shared property for backwards compatibility with QueueRequest
            Change = change;
            
            // Build message locally to avoid race conditions with concurrent executions
            string message = GetMessageString(change);
            
            Logger.WriteLine(
                $"[{change.CorrelationId}] Notification prepared. About to send HTTP request. (Notification.Run)",
                LogLevel.DEBUG);
            
            try
            {
                // Pass both change and message as parameters to avoid race conditions
                // Use GetAwaiter().GetResult() instead of .Result to avoid
                // AggregateException wrapping and potential deadlocks
                Response? response = SendAsync(change, message).GetAwaiter().GetResult();
                if (response != null)
                {
                    Logger.WriteLine($"[{change.CorrelationId}] Response: {response.StatusCode}. URL: {response.Url}. Content: {response.Content}");
                }
            }
            catch (UriFormatException e)
            {
                Logger.WriteLine($"[{change.CorrelationId}] {e.Message}", LogLevel.ERROR);
                return;
            }
            catch (InvalidOperationException e)
            {
                Logger.WriteLine($"[{change.CorrelationId}] {e.Message}", LogLevel.ERROR);
                return;
            }

            // Log exit with correlation ID for traceability
            Logger.WriteLine(
                $"[{change.CorrelationId}] Notification.Run() completed for file: {change.FullPath} (Notification.Run)",
                LogLevel.DEBUG);
        }
    }
}
