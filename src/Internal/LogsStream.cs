using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Serilog.Sinks.GrafanaLoki.Internal;

internal class LogsStream
{
    [JsonPropertyName("stream")]
    public Dictionary<string, string> Labels { get; } = new ();

    [JsonIgnore]
    public Dictionary<string, string> Entries { get; } = new ();

    [JsonPropertyName("values")]
    public List<List<string>> Values
        => Entries.Select(item => new List<string> { item.Key, item.Value }).ToList();
}