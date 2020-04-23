using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Sinks.Http;

namespace Serilog.Sinks.GrafanaLoki
{
    public class GrafanaLokiBatchFormatter : IBatchFormatter
    {
        private readonly IEnumerable<GrafanaLokiLabel> _globalLabels;

        public GrafanaLokiBatchFormatter()
        {
            _globalLabels = new List<GrafanaLokiLabel>();
        }

        public GrafanaLokiBatchFormatter(IEnumerable<GrafanaLokiLabel> labels)
        {
            _globalLabels = labels;
        }

        public void Format(IEnumerable<LogEvent> logEvents, ITextFormatter formatter, TextWriter output)
        {
            if (logEvents == null)
            {
                throw new ArgumentNullException(nameof(logEvents));
            }
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }
            if (!logEvents.Any())
            {
                return;
            }

            var batch = new GrafanaLokiBatch();
            foreach (LogEvent logEvent in logEvents)
            {
                var stream = new GrafanaLokiStream();
                batch.Streams.Add(stream);
                stream.Labels.AddOrReplace("level", GetLevel(logEvent.Level));
                foreach (var item in _globalLabels)
                {
                    stream.Labels.AddOrReplace(item.Key, item.Value);
                }
                foreach (KeyValuePair<string, LogEventPropertyValue> property in logEvent.Properties)
                {
                    // Some enrichers pass strings with quotes surrounding the values inside the string,
                    // which results in redundant quotes after serialization and a "bad request" response.
                    // To avoid this, remove all quotes from the value.
                    stream.Labels.AddOrReplace(property.Key, property.Value.ToString().Replace("\"", ""));
                }

                var sb = new StringBuilder();
                sb.AppendLine(logEvent.RenderMessage());
                if (logEvent.Exception != null)
                {
                    var e = logEvent.Exception;
                    while (e != null)
                    {
                        sb.AppendLine(e.Message);
                        sb.AppendLine(e.StackTrace);
                        e = e.InnerException;
                    }
                }
                stream.Entries.AddOrAppend(Helpers.GetUnixTimestamp(), sb.ToString().TrimEnd(new char[] { '\r', '\n' }));
            }

            if (batch.Streams.Count > 0)
            {
                output.Write(batch.Serialize());
            }
        }

        public void Format(IEnumerable<string> logEvents, TextWriter output)
        {
            if (logEvents == null)
            {
                throw new ArgumentNullException(nameof(logEvents));
            }
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }
            if (!logEvents.Any())
            {
                return;
            }

            var batch = new GrafanaLokiBatch();
            foreach (string logEvent in logEvents)
            {
                if (string.IsNullOrEmpty(logEvent))
                {
                    continue;
                }

                var stream = new GrafanaLokiStream();
                batch.Streams.Add(stream);
                foreach (var item in _globalLabels)
                {
                    stream.Labels.AddOrReplace(item.Key, item.Value);
                }

                stream.Entries.AddOrAppend(Helpers.GetUnixTimestamp(), logEvent.TrimEnd(new char[] { '\r', '\n' }));
            }

            if (batch.Streams.Count > 0)
            {
                output.Write(batch.Serialize());
            }
        }

        private static string GetLevel(LogEventLevel level)
        {
            if (level == LogEventLevel.Information)
            {
                return "info";
            }
            return level.ToString().ToLower();
        }
    }
}
