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

        internal static async Task<HttpResponseMessage> SendAsync(HttpMethod method, Uri uri, List<Header> headers, string body)
        {
            if (uri == null)
            {
                return null;
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
