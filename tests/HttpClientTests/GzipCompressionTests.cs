using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Serilog.Sinks.GrafanaLoki.Tests.HttpClientTests;

public class GzipCompressionTests
{
    private class CapturingHandler : HttpMessageHandler
    {
        public string CapturedContentEncoding { get; private set; }
        public MemoryStream CapturedRequestContent { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedContentEncoding = request.Content?.Headers.ContentEncoding?.ToString();
            if (request.Content != null)
            {
                await request.Content.CopyToAsync(CapturedRequestContent);
            }
            CapturedRequestContent.Position = 0;
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }
    }

    [Fact]
    public async Task GzipCompressedPayload_CanBeDecompressed()
    {
        var handler = new CapturingHandler();
        using var httpClient = new HttpClient(handler);
        using var client = new GrafanaLokiHttpClient(httpClient) { UseGzipCompression = true };
        var json = "{\"streams\":[{\"stream\":{\"level\":\"error\"},\"values\":[[\"1234567890000000000\",\"test message\"]]}]}";
        using var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        await client.PostAsync("http://localhost/test", sourceStream);

        Assert.Equal("gzip", handler.CapturedContentEncoding);

        using var decompressed = new MemoryStream();
        using (var gzip = new GZipStream(handler.CapturedRequestContent, CompressionMode.Decompress))
        {
            gzip.CopyTo(decompressed);
        }
        var result = Encoding.UTF8.GetString(decompressed.ToArray());

        Assert.Equal(json, result);
    }

    [Fact]
    public async Task NoCompression_WhenGzipDisabled()
    {
        var handler = new CapturingHandler();
        using var httpClient = new HttpClient(handler);
        using var client = new GrafanaLokiHttpClient(httpClient);
        var json = "{\"test\":\"data\"}";
        using var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        await client.PostAsync("http://localhost/test", sourceStream);

        Assert.True(string.IsNullOrEmpty(handler.CapturedContentEncoding));
    }
}
