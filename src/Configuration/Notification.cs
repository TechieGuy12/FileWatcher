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
                return;
            }

            if (Triggers.Current.HasFlag(trigger))
            {                
                Change = change;
                _message.Append(GetMessageString(Change));
            }            
        }

        /// <summary>
        /// Send the notification request.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the URL is null or empty.
        /// </exception>
        /// <exception cref="UriFormatException">
        /// Thrown when the URL is not in a valid format.
        /// </exception>
        internal async Task<Response?> SendAsync()
        {
            // If there isn't a message to be sent, then just return
            if (_message == null || _message.Length <= 0)
            {
                return null;
            }

            Uri uri = GetUri();
            
            Data ??= new Data();
            if (Data.Headers != null)
            {
                Data.Headers.Variables ??= new Variables();
                Data.Headers.Variables.Add(Variables?.AllVariables);
                Data.Headers.Change = Change;
            }

            string? content = string.Empty;
            if (Data.Body != null)
            {
                content = Data.Body.Replace("[message]", _message.ToString(), StringComparison.OrdinalIgnoreCase);

                if (Change != null)
                {
                    content = Placeholder.ReplacePlaceholders(
                        content,
                        Change.WatchPath,
                        Change.FullPath,
                        Change.OldPath,
                        Variables?.AllVariables);
                }
            }
           
            Logger.WriteLine($"Sending request: {Method} {uri}.");
            Response response =
                await Request.SendAsync(
                    Method,
                    uri,
                    Data.Headers,
                    content,
                    Data.MimeType).ConfigureAwait(false);

            _message.Clear();
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
        /// <exception cref="InvalidOperationException">
        /// Thrown when either the watch path or the change information was not provided.
        /// </exception>
        /// <exception cref="UriFormatException">
        /// Thrown if the URL is not in a valid format.
        /// </exception>
        private Uri GetUri()
        {
            if (Change == null)
            {
                throw new InvalidOperationException("The change information cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(Url))
            {
                throw new UriFormatException();
            }

            string? url = Placeholder.ReplacePlaceholders(
                Url,
                Change.WatchPath,
                Change.FullPath,
                Change.OldPath,
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

            Change = change;
            _message.Append(GetMessageString(Change));
            
            try
            {
                // This is a console app and for the step functionality to work,
                // sending a notification will need to be sychronous so the next
                // line will block execution and return when completed
                Response? response = SendAsync().Result;
                if (response != null)
                {
                    Logger.WriteLine($"Response: {response.StatusCode}. URL: {response.Url}. Content: {response.Content}");
                }

            }
            catch (AggregateException aex)
            {
                foreach (Exception ex in aex.Flatten().InnerExceptions)
                {
                    Logger.WriteLine(ex.Message, LogLevel.ERROR);
                    Logger.WriteLine(
                        $"StackTrace:{Environment.NewLine}{ex.StackTrace}",
                        LogLevel.ERROR);
                }
            }
            catch (NullReferenceException ex)
            {
                Logger.WriteLine(ex.Message, LogLevel.ERROR);
                Logger.WriteLine(
                    $"StackTrace:{Environment.NewLine}{ex.StackTrace}",
                    LogLevel.ERROR);
            }
        }
    }
}
