using System.Collections.Generic;
using System.Linq;

namespace Serilog.Sinks.GrafanaLoki.Internal;

internal class DictionaryComparer<TKey, TValue> : IEqualityComparer<IDictionary<TKey, TValue>>
{
    public static DictionaryComparer<TKey, TValue> Instance { get; } = new();

    public bool Equals(IDictionary<TKey, TValue>? x, IDictionary<TKey, TValue>? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }

        return x.Count == y.Count && x.All(kvp =>
            y.TryGetValue(kvp.Key, out var value)
            && EqualityComparer<TValue>.Default.Equals(kvp.Value, value));
    }

    public int GetHashCode(IDictionary<TKey, TValue>? obj)
    {
        if (obj is null)
        {
            return 0;
        }

        // Overflow is fine, just wrap
        unchecked
        {
            var hash = 17;
            foreach (var kvp in obj.OrderBy(kvp => kvp.Key))
            {
                hash = hash * 27 + kvp.Key!.GetHashCode();
                hash = hash * 27 + kvp.Value!.GetHashCode();
            }

            return hash;
        }
    }
}