using System.Text.RegularExpressions;
using Serilog.Sinks.GrafanaLoki.Tests.Infrastructure;
using Shouldly;
using Xunit;

namespace Serilog.Sinks.GrafanaLoki.Tests.HttpClientTests;

public class PostContent : IClassFixture<HttpClientTestFixture>
{
    private readonly TestHttpClient _client;

    public PostContent()
    {
        _client = new TestHttpClient();
    }

    [Fact]
    public void ContentMatchesApproved()
    {
        // Arrange
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80", outputTemplate: "{Message:lj}", httpClient: _client)
            .CreateLogger();

        // Act
        log.Error("Something's wrong");
        log.Dispose();

        // Assert
        _client.Content.ShouldMatchApproved(x => x.WithScrubber(s => Regex.Replace(s, "\"[0-9]{19}\"", "\"<unixtimestamp>\"")));
    }

    [Fact]
    public void MultiContentMatchesApproved()
    {
        // Arrange
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80", outputTemplate: "{Message:lj}", httpClient: _client)
            .CreateLogger();

        // Act
        log.Error("Something's wrong");
        log.Warning("Something else is wrong");
        log.Dispose();

        // Assert
        _client.Content.ShouldMatchApproved(x => x.WithScrubber(s => Regex.Replace(s, "\"[0-9]{19}\"", "\"<unixtimestamp>\"")));
    }
}