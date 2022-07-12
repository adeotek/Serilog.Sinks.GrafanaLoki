using System.Collections.Generic;
using System.Linq;
using Serilog.Sinks.GrafanaLoki.Common;

namespace Serilog.Sinks.GrafanaLoki;

public struct LogEventEntry
{
    private long _size = -1;
    private int _labelsCount = 0;

    public string Message { get; }
    public Dictionary<string, string> Labels { get; } = new();

    public LogEventEntry(string message)
    {
        Message = message;
    }

    public long GetByteSize()
    {
        if (Labels.Count == _labelsCount)
        {
            return _size;
        }

        _labelsCount = Labels.Count;
        _size = ByteSize.From(Message) +  Labels.Sum(item => ByteSize.From(item.Key) + ByteSize.From(item.Value));
        return _size;
    }
}