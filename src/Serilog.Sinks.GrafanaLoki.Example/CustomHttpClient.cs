using System;
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
            Console.WriteLine($"StatusCode: {(int)response.StatusCode} - {response.ReasonPhrase}");
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            Console.WriteLine($"Body: {body}");
            return response;
        }
    }
}
