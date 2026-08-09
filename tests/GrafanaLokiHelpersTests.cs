using Serilog.Sinks.GrafanaLoki.Common;
using Xunit;

namespace Serilog.Sinks.GrafanaLoki.Tests;

public class GrafanaLokiHelpersTests
{
    [Theory]
    [InlineData("http://localhost:3100", null, "http://localhost:3100/loki/api/v1/push")]
    [InlineData("http://localhost:3100", "v2", "http://localhost:3100/loki/api/v2/push")]
    [InlineData("http://localhost:3100/", null, "http://localhost:3100/loki/api/v1/push")]
    [InlineData("http://localhost:3100/", "v2", "http://localhost:3100/loki/api/v2/push")]
    [InlineData("https://loki.example.com", null, "https://loki.example.com/loki/api/v1/push")]
    public void BuildPostUri_ConstructsCorrectUrl(string url, string apiVersion, string expected)
    {
        var result = GrafanaLokiHelpers.BuildPostUri(url, apiVersion);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void LogLevelLabelName_IsLevel()
    {
        Assert.Equal("level", GrafanaLokiHelpers.LogLevelLabelName);
    }

    [Fact]
    public void ExceptionTypeLabelName_IsExceptionType()
    {
        Assert.Equal("exception_type", GrafanaLokiHelpers.ExceptionTypeLabelName);
    }

    [Fact]
    public void ExceptionLabelName_IsException()
    {
        Assert.Equal("exception", GrafanaLokiHelpers.ExceptionLabelName);
    }

    [Fact]
    public void DefaultOutputTemplate_ContainsExpectedPlaceholders()
    {
        Assert.Contains("{Timestamp", GrafanaLokiHelpers.DefaultOutputTemplate);
        Assert.Contains("{Level", GrafanaLokiHelpers.DefaultOutputTemplate);
        Assert.Contains("{Message", GrafanaLokiHelpers.DefaultOutputTemplate);
        Assert.Contains("{Exception", GrafanaLokiHelpers.DefaultOutputTemplate);
    }
}
