using Serilog.Sinks.GrafanaLoki.Common;
using Shouldly;
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

        result.ShouldBe(expected);
    }

    [Fact]
    public void LogLevelLabelName_IsLevel()
    {
        GrafanaLokiHelpers.LogLevelLabelName.ShouldBe("level");
    }

    [Fact]
    public void ExceptionTypeLabelName_IsExceptionType()
    {
        GrafanaLokiHelpers.ExceptionTypeLabelName.ShouldBe("exception_type");
    }

    [Fact]
    public void ExceptionLabelName_IsException()
    {
        GrafanaLokiHelpers.ExceptionLabelName.ShouldBe("exception");
    }

    [Fact]
    public void DefaultOutputTemplate_ContainsExpectedPlaceholders()
    {
        GrafanaLokiHelpers.DefaultOutputTemplate.ShouldContain("{Timestamp");
        GrafanaLokiHelpers.DefaultOutputTemplate.ShouldContain("{Level");
        GrafanaLokiHelpers.DefaultOutputTemplate.ShouldContain("{Message");
        GrafanaLokiHelpers.DefaultOutputTemplate.ShouldContain("{Exception");
    }
}
