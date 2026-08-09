using Serilog.Sinks.GrafanaLoki.Common;
using Xunit;
using Encoding = Serilog.Sinks.GrafanaLoki.Common.Encoding;

namespace Serilog.Sinks.GrafanaLoki.Tests.CommonTests;

public class HelpersTests
{
    [Fact]
    public void AddOrReplace_AddsKeyWhenNotPresent()
    {
        var dict = new Dictionary<string, string>();

        dict.AddOrReplace("key1", "value1");

        Assert.Single(dict);
        Assert.Equal("value1", dict["key1"]);
    }

    [Fact]
    public void AddOrReplace_ReplacesValueWhenKeyPresent()
    {
        var dict = new Dictionary<string, string> { { "key1", "oldValue" } };

        dict.AddOrReplace("key1", "newValue");

        Assert.Single(dict);
        Assert.Equal("newValue", dict["key1"]);
    }

    [Fact]
    public void AddOrReplace_ReturnsSameDictionary()
    {
        var dict = new Dictionary<string, string>();

        var result = dict.AddOrReplace("key1", "value1");

        Assert.Same(dict, result);
    }

    [Fact]
    public void AddOrAppend_AddsKeyWhenNotPresent()
    {
        var dict = new Dictionary<string, string>();

        dict.AddOrAppend("key1", "value1");

        Assert.Single(dict);
        Assert.Equal("value1", dict["key1"]);
    }

    [Fact]
    public void AddOrAppend_AppendsValueWhenKeyPresent()
    {
        var dict = new Dictionary<string, string> { { "key1", "hello" } };

        dict.AddOrAppend("key1", " world");

        Assert.Single(dict);
        Assert.Equal("hello world", dict["key1"]);
    }

    [Fact]
    public void AddOrAppend_HandlesNullExistingValue()
    {
        var dict = new Dictionary<string, string> { { "key1", null! } };

        dict.AddOrAppend("key1", "value");

        Assert.Equal("value", dict["key1"]);
    }

    [Fact]
    public void AddOrAppend_HandlesNullAppendedValue()
    {
        var dict = new Dictionary<string, string> { { "key1", "existing" } };

        dict.AddOrAppend("key1", null!);

        Assert.Equal("existing", dict["key1"]);
    }

    [Fact]
    public void AddOrAppend_ReturnsSameDictionary()
    {
        var dict = new Dictionary<string, string>();

        var result = dict.AddOrAppend("key1", "value1");

        Assert.Same(dict, result);
    }

    [Fact]
    public void Base64Encode_ReturnsCorrectValue()
    {
        var result = Helpers.Base64Encode("test:password");

        Assert.Equal("dGVzdDpwYXNzd29yZA==", result);
    }

    [Fact]
    public void Base64Encode_HandlesEmptyString()
    {
        var result = Helpers.Base64Encode("");

        Assert.Equal("", result);
    }

    [Fact]
    public void Base64Encode_HandlesUnicode()
    {
        var result = Helpers.Base64Encode("user:paßwörd");

        // Verify round-trip
        var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(result));
        Assert.Equal("user:paßwörd", decoded);
    }

    [Fact]
    public void StreamToString_ReadsEntireStream()
    {
        var input = "Hello, Loki!";
        using var stream = new MemoryStream(Encoding.UTF8WithoutBom.GetBytes(input));

        var result = Helpers.StreamToString(stream);

        Assert.Equal(input, result);
    }

    [Fact]
    public void StreamToString_ResetsPositionBeforeReading()
    {
        var input = "Hello, Loki!";
        using var stream = new MemoryStream(Encoding.UTF8WithoutBom.GetBytes(input));
        stream.Position = 5; // Move position forward

        var result = Helpers.StreamToString(stream);

        Assert.Equal(input, result);
    }
}
