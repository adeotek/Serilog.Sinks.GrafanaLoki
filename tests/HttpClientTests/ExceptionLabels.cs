using System.Text.Json;
using Serilog.Sinks.GrafanaLoki.Tests.Fixtures;
using Serilog.Sinks.GrafanaLoki.Tests.Infrastructure;
using Shouldly;
using Xunit;

namespace Serilog.Sinks.GrafanaLoki.Tests.HttpClientTests;

public class ExceptionLabels : IClassFixture<HttpClientTestFixture>
{
    [Fact]
    public void Default_IncludesExceptionType_ExcludesException()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80", outputTemplate: "{Message:lj}", httpClient: client)
            .CreateLogger();

        try { throw new InvalidOperationException("Test exception message"); }
        catch (Exception ex) { log.Error(ex, "Something went wrong"); }
        log.Dispose();

        var content = JsonDocument.Parse(client.Content);
        var stream = content.RootElement.GetProperty("streams")[0].GetProperty("stream");

        stream.GetProperty("exception_type").GetString().ShouldBe("System.InvalidOperationException");
        stream.TryGetProperty("exception", out _).ShouldBeFalse();
    }

    [Fact]
    public void NoExceptionProducesNoExceptionLabels()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80", outputTemplate: "{Message:lj}", httpClient: client)
            .CreateLogger();

        log.Error("Something's wrong");
        log.Dispose();

        var content = JsonDocument.Parse(client.Content);
        var stream = content.RootElement.GetProperty("streams")[0].GetProperty("stream");

        stream.TryGetProperty("exception_type", out _).ShouldBeFalse();
        stream.TryGetProperty("exception", out _).ShouldBeFalse();
    }

    [Fact]
    public void ExceptionTypeUsesFullName()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80", outputTemplate: "{Message:lj}", httpClient: client)
            .CreateLogger();

        try { throw new ArgumentNullException("testParam"); }
        catch (Exception ex) { log.Error(ex, "Parameter was null"); }
        log.Dispose();

        var content = JsonDocument.Parse(client.Content);
        var stream = content.RootElement.GetProperty("streams")[0].GetProperty("stream");

        stream.GetProperty("exception_type").GetString().ShouldBe("System.ArgumentNullException");
    }

    [Fact]
    public void ExceptionAsLabelEnabled_IncludesExceptionLabel()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80",
                outputTemplate: "{Message:lj}",
                httpClient: client,
                exceptionAsLabel: true)
            .CreateLogger();

        try { throw new InvalidOperationException("Test exception message"); }
        catch (Exception ex) { log.Error(ex, "Something went wrong"); }
        log.Dispose();

        var content = JsonDocument.Parse(client.Content);
        var stream = content.RootElement.GetProperty("streams")[0].GetProperty("stream");

        stream.GetProperty("exception_type").GetString().ShouldBe("System.InvalidOperationException");
        stream.GetProperty("exception").GetString().ShouldContain("Test exception message");
    }

    [Fact]
    public void ExceptionTypeAsLabelDisabled_ExcludesExceptionType()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80",
                outputTemplate: "{Message:lj}",
                httpClient: client,
                exceptionTypeAsLabel: false)
            .CreateLogger();

        try { throw new InvalidOperationException("Test exception"); }
        catch (Exception ex) { log.Error(ex, "Something went wrong"); }
        log.Dispose();

        var content = JsonDocument.Parse(client.Content);
        var stream = content.RootElement.GetProperty("streams")[0].GetProperty("stream");

        stream.TryGetProperty("exception_type", out _).ShouldBeFalse();
    }

    [Fact]
    public void BothExceptionLabelsDisabled_NeitherPresent()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80",
                outputTemplate: "{Message:lj}",
                httpClient: client,
                exceptionTypeAsLabel: false,
                exceptionAsLabel: false)
            .CreateLogger();

        try { throw new InvalidOperationException("Test exception"); }
        catch (Exception ex) { log.Error(ex, "Something went wrong"); }
        log.Dispose();

        var content = JsonDocument.Parse(client.Content);
        var stream = content.RootElement.GetProperty("streams")[0].GetProperty("stream");

        stream.TryGetProperty("exception_type", out _).ShouldBeFalse();
        stream.TryGetProperty("exception", out _).ShouldBeFalse();
    }
}
