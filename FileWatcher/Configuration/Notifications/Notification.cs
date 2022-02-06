using System.Text;
using System.Xml.Serialization;

namespace TE.FileWatcher.Configuration.Notifications
{
    /// <summary>
    /// A notification that will be triggered.
    /// </summary>
    public class Notification
    {
        // The message to send with the request.
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
                HttpMethod method = HttpMethod.Post;
                if (string.IsNullOrEmpty(MethodString))
                {
                    return method;
                }

                try
                {
                    method = (HttpMethod)Enum.Parse(typeof(HttpMethod), MethodString.ToUpper(), true);
                }
                catch (Exception ex)
                    when (ex is ArgumentNullException || ex is ArgumentException || ex is OverflowException)
                {
                    method = HttpMethod.Post;
                }

                return method;
            }
        }

        /// <summary>
        /// Gets or sets the triggers of the request.
        /// </summary>
        [XmlElement("triggers")]
        public Triggers? Triggers { get; set; }

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
        /// Sends the notification.
        /// </summary>
        /// <param name="message">
        /// The value that replaces the <c>[message]</c> placeholder.
        /// </param>
        /// <param name="trigger">
        /// The trigger for the request.
        /// </param>
        internal void QueueRequest(string message, TriggerType trigger)
        {
            if (Triggers == null || Triggers.TriggerList == null || Triggers.TriggerList.Count <= 0)
            {
                return;
            }

            if (Triggers.Current.HasFlag(trigger))
            {
                _message.Append(CleanMessage(message) + @"\n");
            }            
        }

        /// <summary>
        /// Send the notification request.
        /// </summary>
        /// <exception cref="NullReferenceException">
        /// Thrown when the URL is null or empty.
        /// </exception>
        /// <exception cref="UriFormatException">
        /// Thrown when the URL is not in a valid format.
        /// </exception>
        internal async Task<HttpResponseMessage> SendAsync()
        {
            // If there isn't a message to be sent, then just return
            if (_message == null || _message.Length <= 0)
            {
                return new HttpResponseMessage();
            }

            if (GetUri() == null)
            {
                throw new NullReferenceException("The URL is null or empty.");
            }

            if (Data == null)
            {
                throw new NullReferenceException("Data for the request was not provided.");
            }

            string content = string.Empty;
            if (Data.Body != null)
            {
                content = Data.Body.Replace("[message]", _message.ToString());
            }

            HttpResponseMessage response =
                await Request.SendAsync(
                    Method,
                    GetUri(),
                    Data.Headers,
                    content,
                    Data.MimeType);

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
                            t = "000" + string.Format("{0:X}", c);
                            sb.Append(string.Concat("\\u", t.AsSpan(t.Length - 4)));
                            //sb.Append("\\u" + t.Substring(t.Length - 4));
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
        /// <exception cref="UriFormatException">
        /// Thrown if the URL is not in a valid format.
        /// </exception>
        private Uri GetUri()
        {
            if (string.IsNullOrWhiteSpace(Url))
            {
                throw new UriFormatException();
            }

            Uri uri = new(Url);
            return uri;
        }
    }
}
