using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TE.FileWatcher.Notifications
{
    public class Notification
    {
        private HttpClient _httpClient;

        private StringBuilder _message;

        /// <summary>
        /// Gets or sets the URL of the request.
        /// </summary>
        [XmlElement("url")]
        public string Url { get; set; }

        /// <summary>
        /// Gets the URI value of the string URL.
        /// </summary>
        [XmlIgnore]
        public Uri Uri
        {
            get
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(Url))
                    {
                        return null;
                    }

                    Uri uri = new Uri(Url);
                    return uri;
                }
                catch (Exception ex)
                    when (ex is ArgumentNullException || ex is UriFormatException)
                {
                    return null;
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the string representation of the request method.
        /// </summary>
        [XmlElement("method")]
        public string MethodString { get; set; }

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
                    method = (HttpMethod)Enum.Parse(typeof(HttpMethod), MethodString, true);
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
        public Triggers Triggers { get; set; } = new Triggers();

        /// <summary>
        /// Gets or sets the data to send for the request.
        /// </summary>
        [XmlElement("data")]
        public Data Data { get; set; } = new Data();

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
        internal void QueueRequest(string message)
        {
            _message.Append(CleanMessage(message) + @"\n");
        }

        /// <summary>
        /// Send the notification request.
        /// </summary>
        internal async void Send()
        {
            if (Uri == null)
            {
                return;
            }

            if (_message.Length <= 0)
            {
                return;
            }

            string content = Data.Body.Replace("[message]", _message.ToString());
            using (HttpResponseMessage response =
                await Request.SendAsync(Method, Uri, Data.Headers.HeaderList, content))
            {
                Console.WriteLine($"Response: {response.StatusCode}.");
                using (HttpContent httpContent = response.Content)
                {
                    string resultContent = await httpContent.ReadAsStringAsync();
                    Console.WriteLine($"Content: {resultContent}");
                }
            }
            _message.Clear();
        }

        private string CleanMessage(string message)
        {
            //const string reduceMultiSpace = @"[ ]{2,}";
            message = message.Replace(@"\", @"\\").Trim();
            return Regex.Replace(message, @"\r\n?|\n", "\n");
            //return Regex.Replace(body.Replace("\t", ""), reduceMultiSpace, " ");
        }
    }
}
