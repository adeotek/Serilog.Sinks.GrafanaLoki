using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace Serilog.Sinks.GrafanaLoki.Internal;

internal class LogsBatch
{
    [JsonPropertyName("streams")]
    public List<LogsStream> Streams { get; } = new();

    public LogsStream CreateStream()
    {
        var stream = new LogsStream();
        Streams.Add(stream);
        return stream;
    }

    public string Serialize()
    {
        var encoderSettings = new TextEncoderSettings();
        encoderSettings.AllowRange(UnicodeRanges.All);
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(encoderSettings),
            WriteIndented = false
        })
            .Replace("\\u0027", "'")
            .Replace("\\u003C", "<")
            .Replace("\\u003E", ">")
            .Replace("\\u0026", "&");
    }
}