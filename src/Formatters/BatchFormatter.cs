using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog.Sinks.GrafanaLoki.Common;
using Serilog.Sinks.GrafanaLoki.Internal;

namespace Serilog.Sinks.GrafanaLoki.Formatters;

public class BatchFormatter : IBatchFormatter
{
    private readonly Dictionary<string, string> _globalLabels;

    public BatchFormatter(Dictionary<string, string>? labels = null)
    {
        _globalLabels = labels ?? new Dictionary<string, string>();
    }

    public void Format(IEnumerable<LogEventEntry> logEvents, TextWriter output)
    {
        if (logEvents == null)
        {
            throw new ArgumentNullException(nameof(logEvents));
        }

        if (output == null)
        {
            throw new ArgumentNullException(nameof(output));
        }

        var logEventsArray = logEvents as LogEventEntry[] ?? logEvents.ToArray();
        if (!logEventsArray.Any())
        {
            return;
        }

        var batch = new LogsBatch();
        foreach (var group in logEventsArray
                     .Where(e => !string.IsNullOrWhiteSpace(e.Message))
                     .GroupBy(
                         e => e.Labels,
                         e => e,
                         DictionaryComparer<string, string>.Instance))
        {
            var stream = batch.CreateStream();
            foreach (var item in _globalLabels)
            {
                stream.Labels.AddOrReplace(item.Key, item.Value);
            }
            foreach (var label in group.Key)
            {
                stream.Labels.AddOrAppend(label.Key, label.Value);
            }
            foreach (var entry in group.OrderBy(e => e.Timestamp))
            {
                stream.Entries.AddOrAppend(entry.Timestamp.ToString(), entry.Message.TrimEnd('\r', '\n'));
            }
        }

        if (batch.Streams.Count > 0)
        {
            output.Write(batch.Serialize());
        }
    }
}