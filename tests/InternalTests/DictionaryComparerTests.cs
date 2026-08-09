using Serilog.Sinks.GrafanaLoki.Internal;
using Shouldly;
using Xunit;

namespace Serilog.Sinks.GrafanaLoki.Tests.InternalTests;

public class DictionaryComparerTests
{
    [Fact]
    public void Equals_ReturnsTrueForSameReference()
    {
        var dict = new Dictionary<string, string> { { "a", "1" } };

        var result = DictionaryComparer<string, string>.Instance.Equals(dict, dict);

        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_ReturnsTrueForEqualDictionaries()
    {
        var dict1 = new Dictionary<string, string> { { "a", "1" }, { "b", "2" } };
        var dict2 = new Dictionary<string, string> { { "a", "1" }, { "b", "2" } };

        var result = DictionaryComparer<string, string>.Instance.Equals(dict1, dict2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_ReturnsFalseForDifferentValues()
    {
        var dict1 = new Dictionary<string, string> { { "a", "1" } };
        var dict2 = new Dictionary<string, string> { { "a", "2" } };

        var result = DictionaryComparer<string, string>.Instance.Equals(dict1, dict2);

        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_ReturnsFalseForDifferentKeys()
    {
        var dict1 = new Dictionary<string, string> { { "a", "1" } };
        var dict2 = new Dictionary<string, string> { { "b", "1" } };

        var result = DictionaryComparer<string, string>.Instance.Equals(dict1, dict2);

        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_ReturnsFalseForDifferentCount()
    {
        var dict1 = new Dictionary<string, string> { { "a", "1" } };
        var dict2 = new Dictionary<string, string> { { "a", "1" }, { "b", "2" } };

        var result = DictionaryComparer<string, string>.Instance.Equals(dict1, dict2);

        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_ReturnsFalseWhenFirstIsNull()
    {
        var dict = new Dictionary<string, string> { { "a", "1" } };

        var result = DictionaryComparer<string, string>.Instance.Equals(null, dict);

        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_ReturnsFalseWhenSecondIsNull()
    {
        var dict = new Dictionary<string, string> { { "a", "1" } };

        var result = DictionaryComparer<string, string>.Instance.Equals(dict, null);

        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_ReturnsTrueWhenBothAreNull()
    {
        var result = DictionaryComparer<string, string>.Instance.Equals(null, null);

        result.ShouldBeTrue();
    }

    [Fact]
    public void GetHashCode_ReturnsSameValueForEqualDictionaries()
    {
        var dict1 = new Dictionary<string, string> { { "a", "1" }, { "b", "2" } };
        var dict2 = new Dictionary<string, string> { { "b", "2" }, { "a", "1" } }; // Different order

        var hash1 = DictionaryComparer<string, string>.Instance.GetHashCode(dict1);
        var hash2 = DictionaryComparer<string, string>.Instance.GetHashCode(dict2);

        hash1.ShouldBe(hash2);
    }

    [Fact]
    public void GetHashCode_ReturnsDifferentValueForDifferentDictionaries()
    {
        var dict1 = new Dictionary<string, string> { { "a", "1" } };
        var dict2 = new Dictionary<string, string> { { "a", "2" } };

        var hash1 = DictionaryComparer<string, string>.Instance.GetHashCode(dict1);
        var hash2 = DictionaryComparer<string, string>.Instance.GetHashCode(dict2);

        hash1.ShouldNotBe(hash2);
    }

    [Fact]
    public void GetHashCode_ReturnsZeroForNull()
    {
        var result = DictionaryComparer<string, string>.Instance.GetHashCode(null);

        result.ShouldBe(0);
    }
}
