using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Serilog.Sinks.GrafanaLoki.Internal;

internal class LogsStream
{
    [JsonPropertyName("stream")]
    public Dictionary<string, string> Labels { get; } = new ();

    [JsonIgnore]
    public List<StreamEntry> Entries { get; } = new ();

    [JsonPropertyName("values")]
    public List<List<object>> Values
        => Entries.Select(entry =>
        {
            var values = new List<object> { entry.Timestamp, entry.Message };
            if (entry.Metadata is { Count: > 0 })
            {
                values.Add(entry.Metadata);
            }
            return values;
        }).ToList();
}