using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Serilog.Sinks.GrafanaLoki
{
    public class GrafanaLokiHttpClient : IGrafanaLokiHttpClient
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

        public virtual void SetCredentials(GrafanaLokiCredentials credentials)
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

            var token = Helpers.Base64Encode($"{credentials.User}:{credentials.Password ?? string.Empty}");
            headers.Add("Authorization", $"Basic {token}");
        }

        public virtual Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content)
        {
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            return HttpClient.PostAsync(requestUri, content);
        }

        public virtual void Dispose() => HttpClient.Dispose();        
    }
}
