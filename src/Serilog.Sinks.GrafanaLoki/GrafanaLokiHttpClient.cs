using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Serilog.Sinks.Http;

namespace Serilog.Sinks.GrafanaLoki
{
    public class GrafanaLokiHttpClient : IHttpClient
    {
        protected readonly HttpClient HttpClient;

        public GrafanaLokiHttpClient(HttpClient httpClient = null)
        {
            HttpClient = httpClient ?? new HttpClient();
        }

        public GrafanaLokiHttpClient(HttpClient httpClient, GrafanaLokiCredentials credentials)
        {
            HttpClient = httpClient ?? new HttpClient();
            SetCredentials(credentials);
        }

        public void SetCredentials(GrafanaLokiCredentials credentials)
        {
            if (credentials == null || string.IsNullOrEmpty(credentials.User))
            {
                return;
            }
            var headers = HttpClient.DefaultRequestHeaders;
            if (headers.Any(x => x.Key == "Authorization"))
            {
                return;
            }

            var token = Base64Encode($"{credentials.User}:{credentials.Password ?? string.Empty}");
            headers.Add("Authorization", $"Basic {token}");
        }

        public virtual Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content)
        {
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            return HttpClient.PostAsync(requestUri, content);
        }

        public virtual void Dispose() => HttpClient.Dispose();

        private static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }
    }
}
