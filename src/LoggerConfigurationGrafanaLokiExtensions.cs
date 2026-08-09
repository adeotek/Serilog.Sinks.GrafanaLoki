using System;
using System.Collections.Generic;
using Serilog.Configuration;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Sinks.GrafanaLoki.Common;
using Serilog.Sinks.GrafanaLoki.Formatters;

namespace Serilog.Sinks.GrafanaLoki;

/// <summary>Extends <see cref="LoggerConfiguration"/> with methods to add file sinks.</summary>
public static class LoggerConfigurationGrafanaLokiExtensions
{
    /// <summary>
    /// Send log events to Grafana's Loki.
    /// </summary>
    /// <param name="sinkConfiguration">Logger sink configuration.</param>
    /// <param name="url">Base URL for Loki API.</param>
    /// <param name="credentials">Loki Http credentials.</param>
    /// <param name="labels">Log event Labels</param>
    /// <param name="restrictedToMinimumLevel">
    /// The minimum level for events passed through the sink.
    /// </param>
    /// <param name="outputTemplate">
    /// A message template describing the format used to write to the sink.
    /// The default is "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} | [{Level:u3}] | {Message:lj} | {Exception}".
    /// </param>
    /// <param name="propertiesStringDelimiter">
    /// A delimiter used for event string properties. The default values is ` (backtick).
    /// </param>
    /// <param name="formatProvider">
    /// Supplies culture-specific formatting information, or null.
    /// </param>
    /// <param name="batchFormatter">
    /// The formatter batching multiple log events into a payload that can be sent over the
    /// network. Specify null for no limit.
    /// </param>
    /// <param name="queueLimitBytes">
    /// The maximum size, in bytes, of events stored in memory, waiting to be sent over the
    /// network. Specify null for no limit.
    /// </param>
    /// <param name="logEventLimitBytes">
    /// The maximum size, in bytes, for a serialized representation of a log event. Log events
    /// exceeding this size will be dropped. Specify null for no limit. Default value is null.
    /// </param>
    /// <param name="logEventsInBatchLimit">
    /// The maximum number of log events sent as a single batch over the network. Default
    /// value is 1000.
    /// </param>
    /// <param name="batchSizeLimitBytes">
    /// The approximate maximum size, in bytes, for a single batch. The value is an
    /// approximation because only the size of the log events are considered. The extra
    /// characters added by the batch formatter, where the sequence of serialized log events
    /// are transformed into a payload, are not considered. Please make sure to accommodate for
    /// those.
    /// <para />
    /// Default value is null.
    /// </param>
    /// <param name="period">
    /// The time to wait between checking for event batches. Default value is 2 seconds.
    /// </param>
    /// <param name="apiVersion">
    /// Loki API version, default "v1".
    /// </param>
    /// <param name="httpClient">
    /// Custom HttpClient or null or default (IHttpClient).
    /// </param>
    /// <param name="httpRequestTimeout">
    /// HttpRequest timeout where null is the default HttpClient timeout (default null).
    /// </param>
    /// <param name="debugMode">
    /// Debug mod switch on/off.
    /// </param>
    /// <param name="exceptionTypeAsLabel">
    /// When true (default), the exception type (e.g. "System.InvalidOperationException")
    /// is added as a Loki label. This is low-cardinality and safe to enable.
    /// </param>
    /// <param name="exceptionAsLabel">
    /// When true, the full exception string (message and stack trace) is added as a Loki label.
    /// This can cause high label cardinality and is disabled by default.
    /// </param>
    /// <returns>Configuration object allowing method chaining.</returns>
    public static LoggerConfiguration GrafanaLoki(
        this LoggerSinkConfiguration sinkConfiguration,
        string url,
        GrafanaLokiCredentials? credentials = null,
        Dictionary<string, string>? labels = null,
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
        string outputTemplate = GrafanaLokiHelpers.DefaultOutputTemplate,
        string? propertiesStringDelimiter = null,
        IFormatProvider? formatProvider = null,
        IBatchFormatter? batchFormatter = null,
        long? queueLimitBytes = null,
        long? logEventLimitBytes = null,
        int? logEventsInBatchLimit = null,
        long? batchSizeLimitBytes = null,
        TimeSpan? period = null,
        string? apiVersion = null,
        IHttpClient? httpClient = null,
        int? httpRequestTimeout = null,
        bool debugMode = false,
        bool exceptionTypeAsLabel = true,
        bool exceptionAsLabel = false,
        bool useStructuredMetadata = false,
        int? maxLabelCount = null,
        bool useGzipCompression = false)
    {
        if (sinkConfiguration == null)
        {
            throw new ArgumentNullException(nameof(sinkConfiguration));
        }
        if (url == null)
        {
            throw new ArgumentNullException(nameof(url));
        }
        if (outputTemplate == null)
        {
            throw new ArgumentNullException(nameof(outputTemplate));
        }

        // Default values
        logEventsInBatchLimit ??= 1000;
        period ??= TimeSpan.FromSeconds(2);
        var requestUri = GrafanaLokiHelpers.BuildPostUri(url, apiVersion);
        var textFormatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);
        batchFormatter ??= new BatchFormatter(labels);
        if (httpClient == null)
        {
            httpClient ??= new GrafanaLokiHttpClient(null, credentials, httpRequestTimeout ?? -1);
        }
        else if (credentials != null)
        {
            httpClient.SetCredentials(credentials);
        }
        httpClient.DebugMode = debugMode;
        if (httpClient is GrafanaLokiHttpClient grafanaLokiHttpClient)
        {
            grafanaLokiHttpClient.UseGzipCompression = useGzipCompression;
        }
        else if (useGzipCompression)
        {
            SelfLog.WriteLine("useGzipCompression is set to true but the provided HTTP client is not a GrafanaLokiHttpClient; compression will not be applied");
        }

        var sink = new GrafanaLokiHttpSink(
            requestUri: requestUri,
            queueLimitBytes: queueLimitBytes,
            logEventLimitBytes,
            logEventsInBatchLimit,
            batchSizeLimitBytes,
            period.Value,
            propertiesStringDelimiter,
            textFormatter,
            batchFormatter,
            httpClient,
            exceptionTypeAsLabel,
            exceptionAsLabel,
            useStructuredMetadata,
            maxLabelCount);

        return sinkConfiguration.Sink(sink, restrictedToMinimumLevel);
    }
}