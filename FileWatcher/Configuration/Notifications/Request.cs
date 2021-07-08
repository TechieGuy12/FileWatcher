using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TE.FileWatcher.Configuration.Notifications
{
    internal static class Request
    {
        /// <summary>
        /// The MIME type used for the request.
        /// </summary>
        internal enum MimeType
        {
            /// <summary>
            /// JSON
            /// </summary>
            Json,
            /// <summary>
            /// XML
            /// </summary>
            Xml
        }

        /// <summary>
        /// The valid JSON name.
        /// </summary>
        internal const string JSON_NAME = "JSON";

        /// <summary>
        /// The valid XML name.
        /// </summary>
        internal const string XML_NAME = "XML";

        // JSON mime type
        private const string MIME_TYPE_JSON = "application/json";

        // XML mime type
        private const string MIME_TYPE_XML = "application/xml";

        // The HTTP client
        private static HttpClient _httpClient;

        /// <summary>
        /// Sends a request to a remote system asychronously.
        /// </summary>
        /// <param name="method"></param>
        /// The HTTP method to use for the request.
        /// <param name="uri"></param>
        /// The URL of the request.
        /// <param name="headers"></param>
        /// A <see cref="List{T}"/> of <see cref="Header"/> objects associated
        /// with the request.
        /// <param name="body">
        /// The content body of the request.
        /// </param>
        /// <param name="mimeType">
        /// The MIME type associated with the request.
        /// </param>
        /// <returns>
        /// The response message of the request.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when an argument is null or empty.
        /// </exception>
        internal static async Task<HttpResponseMessage> SendAsync(
            HttpMethod method,
            Uri uri,
            List<Header> headers,
            string body,
            MimeType mimeType)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (_httpClient == null)
            {
                _httpClient = new HttpClient();
            }

            HttpRequestMessage request = new HttpRequestMessage(method, uri);
            foreach (Header header in headers)
            {
                request.Headers.Add(header.Name, header.Value);
            }
            request.Content = new StringContent(body, Encoding.UTF8, GetMimeTypeString(mimeType));

            HttpResponseMessage response = null;
            try
            {
                response = await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                if (response == null)
                {
                    response = new HttpResponseMessage();
                }

                response.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                response.ReasonPhrase = $"Request could not be sent. Reason: {ex.Message}";
            }

            return response;
        }

        /// <summary>
        /// Gets the string value of the specified MIME type.
        /// </summary>
        /// <param name="mimeType">
        /// The MIME type used for the request.
        /// </param>
        /// <returns>
        /// The string value of the specified MIME type.
        /// </returns>
        private static string GetMimeTypeString(MimeType mimeType)
        {
            string type = MIME_TYPE_JSON;
            if (mimeType == MimeType.Xml)
            {
                type = MIME_TYPE_XML;
            }

            return type;
        }
    }
}
