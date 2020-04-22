using System.Net.Http;
using System.Threading.Tasks;

namespace Serilog.Sinks.GrafanaLoki.Example
{
    public class CustomHttpClient : GrafanaLokiHttpClient
    {
        public override async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content)
        {
            var req = content.ReadAsStringAsync().Result;
            var response = await base.PostAsync(requestUri, content);
            var body = response.Content.ReadAsStringAsync().Result;
            return response;
        }
    }
}
