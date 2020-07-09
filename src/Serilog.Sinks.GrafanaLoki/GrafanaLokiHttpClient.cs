using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Serilog.Sinks.GrafanaLoki
{
    public class GrafanaLokiHttpClient : IGrafanaLokiHttpClient
    {
        protected readonly HttpClient HttpClient;
        public bool DebugMode { get; set; }

        public GrafanaLokiHttpClient(HttpClient httpClient = null)
        {
            HttpClient = httpClient ?? new HttpClient();
        }

        public GrafanaLokiHttpClient(HttpClient httpClient, GrafanaLokiCredentials credentials, int httpTimeout = -1)
        {
            HttpClient = httpClient ?? new HttpClient();
            if (httpTimeout > 0)
            {
                HttpClient.Timeout = new TimeSpan(0,0,0,0, httpTimeout);
            }
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

        public virtual async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content)
        {
            HttpResponseMessage response;
            try
            {
                content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                response = await HttpClient.PostAsync(requestUri, content);
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    if (DebugMode)
                    {
                        Console.WriteLine($"GrafanaLoki sending data: {content.ReadAsStringAsync().GetAwaiter().GetResult()}");
                        Console.WriteLine($"GrafanaLoki response StatusCode: {(int)response.StatusCode} - {response.ReasonPhrase}");
                        if (response.StatusCode != HttpStatusCode.NoContent && response.StatusCode != HttpStatusCode.OK)
                        {
                            Console.WriteLine($"GrafanaLoki response body: {body}");
                        }
                    }
                    if (body.Contains("error parsing labels:") || body.Contains("ignored, reason: 'entry out of order' for stream"))
                    {
                        Log.Warning("Bad request Loki response: {msg}", body);
                        response.StatusCode = HttpStatusCode.OK;
                        if (DebugMode)
                        {
                            Console.WriteLine($"GrafanaLoki error suppressed!!!");
                        }
                    }
                } else if (DebugMode)
                {
                    Console.WriteLine($"GrafanaLoki sending data: {content.ReadAsStringAsync().GetAwaiter().GetResult()}");
                    Console.WriteLine($"GrafanaLoki response StatusCode: {(int)response.StatusCode} - {response.ReasonPhrase}");
                    if (response.StatusCode != HttpStatusCode.NoContent && response.StatusCode != HttpStatusCode.OK)
                    {
                        var body = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"GrafanaLoki response body: {body}");
                    }
                }
            }
            catch (Exception ex)
            {
                if (DebugMode)
                {
                    Console.WriteLine($"GrafanaLoki POST exception: {ex.Message}");
                }
                throw ex;
            }
            return response;
        }

        public virtual void Dispose() => HttpClient.Dispose();        
    }
}
