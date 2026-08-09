using Serilog.Sinks.GrafanaLoki.Common;

namespace Serilog.Sinks.GrafanaLoki.Tests.Infrastructure;

public class TestHttpClient : GrafanaLokiHttpClient
{
    public override Task<HttpResponseMessage> PostAsync(string requestUri, System.IO.Stream contentStream)
    {
        Content = Helpers.StreamToString(contentStream);
        RequestUri = requestUri;

        return Task.FromResult(new HttpResponseMessage());
    }

    public HttpClient Client => HttpClient;

    public string Content { get; set; } = string.Empty;

    public string RequestUri { get; set; } = string.Empty;
}