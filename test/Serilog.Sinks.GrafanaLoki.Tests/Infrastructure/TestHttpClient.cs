using System.Net.Http;
using System.Threading.Tasks;

namespace Serilog.Sinks.GrafanaLoki.Tests.Infrastructure
{
    public class TestHttpClient : GrafanaLokiHttpClient
    {
        public override async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content)
        {
            Content = await content.ReadAsStringAsync();
            RequestUri = requestUri;

            return new HttpResponseMessage();
        }

        public HttpClient Client => HttpClient;

        public string Content;

        public string RequestUri;
    }
}
