using Serilog.Sinks.GrafanaLoki.Common;
using Shouldly;
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

        dict.ShouldHaveSingleItem();
        dict["key1"].ShouldBe("value1");
    }

    [Fact]
    public void AddOrReplace_ReplacesValueWhenKeyPresent()
    {
        var dict = new Dictionary<string, string> { { "key1", "oldValue" } };

        dict.AddOrReplace("key1", "newValue");

        dict.ShouldHaveSingleItem();
        dict["key1"].ShouldBe("newValue");
    }

    [Fact]
    public void AddOrReplace_ReturnsSameDictionary()
    {
        var dict = new Dictionary<string, string>();

        var result = dict.AddOrReplace("key1", "value1");

        result.ShouldBeSameAs(dict);
    }

    [Fact]
    public void AddOrAppend_AddsKeyWhenNotPresent()
    {
        var dict = new Dictionary<string, string>();

        dict.AddOrAppend("key1", "value1");

        dict.ShouldHaveSingleItem();
        dict["key1"].ShouldBe("value1");
    }

    [Fact]
    public void AddOrAppend_AppendsValueWhenKeyPresent()
    {
        var dict = new Dictionary<string, string> { { "key1", "hello" } };

        dict.AddOrAppend("key1", " world");

        dict.ShouldHaveSingleItem();
        dict["key1"].ShouldBe("hello world");
    }

    [Fact]
    public void AddOrAppend_HandlesNullExistingValue()
    {
        var dict = new Dictionary<string, string> { { "key1", null! } };

        dict.AddOrAppend("key1", "value");

        dict["key1"].ShouldBe("value");
    }

    [Fact]
    public void AddOrAppend_HandlesNullAppendedValue()
    {
        var dict = new Dictionary<string, string> { { "key1", "existing" } };

        dict.AddOrAppend("key1", null!);

        dict["key1"].ShouldBe("existing");
    }

    [Fact]
    public void AddOrAppend_ReturnsSameDictionary()
    {
        var dict = new Dictionary<string, string>();

        var result = dict.AddOrAppend("key1", "value1");

        result.ShouldBeSameAs(dict);
    }

    [Fact]
    public void Base64Encode_ReturnsCorrectValue()
    {
        var result = Helpers.Base64Encode("test:password");

        result.ShouldBe("dGVzdDpwYXNzd29yZA==");
    }

    [Fact]
    public void Base64Encode_HandlesEmptyString()
    {
        var result = Helpers.Base64Encode("");

        result.ShouldBe("");
    }

    [Fact]
    public void Base64Encode_HandlesUnicode()
    {
        var result = Helpers.Base64Encode("user:paßwörd");

        // Verify round-trip
        var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(result));
        decoded.ShouldBe("user:paßwörd");
    }

    [Fact]
    public void StreamToString_ReadsEntireStream()
    {
        var input = "Hello, Loki!";
        using var stream = new MemoryStream(Encoding.UTF8WithoutBom.GetBytes(input));

        var result = Helpers.StreamToString(stream);

        result.ShouldBe(input);
    }

    [Fact]
    public void StreamToString_ResetsPositionBeforeReading()
    {
        var input = "Hello, Loki!";
        using var stream = new MemoryStream(Encoding.UTF8WithoutBom.GetBytes(input));
        stream.Position = 5; // Move position forward

        var result = Helpers.StreamToString(stream);

        result.ShouldBe(input);
    }
}
