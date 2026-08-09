using Serilog.Sinks.GrafanaLoki.Common;
using Serilog.Sinks.GrafanaLoki.Tests.Fixtures;
using Serilog.Sinks.GrafanaLoki.Tests.Infrastructure;
using Xunit;

namespace Serilog.Sinks.GrafanaLoki.Tests.HttpClientTests;

public class AuthTests : IClassFixture<HttpClientTestFixture>
{
    private readonly TestHttpClient _client;

    public AuthTests()
    {
        _client = new TestHttpClient();
    }

    [Fact]
    public void BasicAuthHeaderIsCorrect()
    {
        // Arrange
        var credentials = new GrafanaLokiCredentials { User = "<user>", Password = "<password>" };
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80", credentials, httpClient: _client)
            .CreateLogger();

        // Act
        log.Error("Something's wrong");
        log.Dispose();

        // Assert
        var auth = _client.Client.DefaultRequestHeaders.Authorization;
        Assert.Equal("Basic", auth?.Scheme);
        Assert.Equal(Helpers.Base64Encode($"{credentials.User}:{credentials.Password}"), auth?.Parameter);
    }

    [Fact]
    public void NoAuthHeaderIsCorrect()
    {
        // Arrange
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80", httpClient: _client)
            .CreateLogger();

        // Act
        log.Error("Something's wrong");
        log.Dispose();

        // Assert
        Assert.Null(_client.Client.DefaultRequestHeaders.Authorization);
    }
}
