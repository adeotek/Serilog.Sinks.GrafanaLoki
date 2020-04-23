using Serilog.Sinks.GrafanaLoki.Tests.Infrastructure;
using Shouldly;
using Xunit;

namespace Serilog.Sinks.GrafanaLoki.Tests.HttpClientTests
{
    public class RequestUriTests
    {
        private readonly TestHttpClient _client;

        public RequestUriTests()
        {
            _client = new TestHttpClient();
        }

        [Theory]
        [InlineData("http://test:80")]
        [InlineData("http://test:80/")]
        public void RequestUriIsCorrect(string url)
        {
            // Arrange
            var log = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.GrafanaLoki(url, httpClient: _client)
                .CreateLogger();

            // Act
            log.Error("Something's wrong");
            log.Dispose();

            // Assert
            _client.RequestUri.ShouldBe(LoggerConfigurationGrafanaLokiExtensions.BuildPostUri(url));
        }
    }
}
