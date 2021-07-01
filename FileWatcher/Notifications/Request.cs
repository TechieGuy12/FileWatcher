using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TE.FileWatcher.Notifications
{
    internal static class Request
    {
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
        /// <returns>
        /// The response message of the request.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when an argument is null or empty.
        /// </exception>
        internal static async Task<HttpResponseMessage> SendAsync(HttpMethod method, Uri uri, List<Header> headers, string body)
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
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");

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
    }
}
