using Microsoft.Extensions.DependencyInjection;
using System.Text;
using TE.FileWatcher.Configuration;

namespace TE.FileWatcher.Net
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

        // The collection of services - contains the HTTP clients
        private static readonly ServiceCollection _services = new ServiceCollection();

        // The provider of the service - the HTTP client
        private static ServiceProvider? _serviceProvider;

        /// <summary>
        /// Sends a request to a remote system asychronously.
        /// </summary>
        /// <param name="method"></param>
        /// The HTTP method to use for the request.
        /// <param name="uri"></param>
        /// The URL of the request.
        /// <param name="headers"></param>
        /// The <see cref="Headers"/> object associated with the request.
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
        internal static async Task<Response> SendAsync(
            HttpMethod method,
            Uri uri,
            Headers? headers,
            string? body,
            MimeType mimeType)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (_serviceProvider == null)
            {
                _services.AddHttpClient();
                _serviceProvider = _services.BuildServiceProvider();
            }

            using (HttpRequestMessage request = new HttpRequestMessage(method, uri))
            {
                headers?.Set(request);

                if (body != null)
                {
                    request.Content = new StringContent(body, Encoding.UTF8, GetMimeTypeString(mimeType));
                }

                try
                {
                    var client = _serviceProvider.GetService<HttpClient>();
                    if (client != null)
                    {
                        using (HttpResponseMessage requestResponse =
                            await client.SendAsync(request).ConfigureAwait(false))
                        {
                            using (HttpContent httpContent = requestResponse.Content)
                            {
                                string resultContent =
                                    await httpContent.ReadAsStringAsync().ConfigureAwait(false);

                                return new Response(
                                    requestResponse.StatusCode,
                                    requestResponse.ReasonPhrase,
                                    resultContent);
                            }
                        }
                    }
                    else
                    {
                        return new Response(
                            System.Net.HttpStatusCode.InternalServerError,
                            "Request could not be sent. Reason: The HTTP client service could not be initialized.",
                            null); ;
                    }
                }
                catch (Exception ex)
                    when (ex is ArgumentNullException || ex is InvalidOperationException || ex is HttpRequestException || ex is TaskCanceledException)
                {
                    return new Response(
                        System.Net.HttpStatusCode.InternalServerError,
                        $"Request could not be sent. Reason: {ex.Message}",
                        null);
                }                
            }            
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
