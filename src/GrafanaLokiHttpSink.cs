using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Sinks.GrafanaLoki.Common;
using Serilog.Sinks.GrafanaLoki.Formatters;
using Serilog.Sinks.GrafanaLoki.Internal;

namespace Serilog.Sinks.GrafanaLoki;

public class GrafanaLokiHttpSink : ILogEventSink, IDisposable
{
    private readonly string _requestUri;
    private readonly long? _logEventLimitBytes;
    private readonly int? _logEventsInBatchLimit;
    private readonly long? _batchSizeLimitBytes;
    private readonly ITextFormatter _textFormatter;
    private readonly string? _propertiesDelimiter;
    private readonly IBatchFormatter _batchFormatter;
    private readonly IHttpClient _httpClient;
    private readonly ExponentialBackoffConnectionSchedule _connectionSchedule;
    private readonly PortableTimer _timer;
    private readonly object _syncRoot = new ();
    private readonly LogEventsQueue _queue;

    private LogEventsBatch? _unsentBatch;
    private volatile bool _disposed;

    public GrafanaLokiHttpSink(
        string requestUri,
        long? queueLimitBytes,
        long? logEventLimitBytes,
        int? logEventsInBatchLimit,
        long? batchSizeLimitBytes,
        TimeSpan period,
        string? propertiesDelimiter,
        ITextFormatter textFormatter,
        IBatchFormatter batchFormatter,
        IHttpClient httpClient)
    {
        _requestUri = requestUri ?? throw new ArgumentNullException(nameof(requestUri));
        _logEventLimitBytes = logEventLimitBytes;
        _logEventsInBatchLimit = logEventsInBatchLimit;
        _batchSizeLimitBytes = batchSizeLimitBytes;
        _textFormatter = textFormatter ?? throw new ArgumentNullException(nameof(textFormatter));
        _propertiesDelimiter = propertiesDelimiter;
        _batchFormatter = batchFormatter ?? throw new ArgumentNullException(nameof(batchFormatter));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        _connectionSchedule = new ExponentialBackoffConnectionSchedule(period);
        _timer = new PortableTimer(OnTick);
        _queue = new LogEventsQueue(queueLimitBytes);

        SetTimer();
    }

    public void Emit(LogEvent logEvent)
    {
        if (logEvent == null)
        {
            throw new ArgumentNullException(nameof(logEvent));
        }

        var writer = new StringWriter();
        _textFormatter.Format(logEvent, writer);
        var entry = new LogEventEntry(writer.ToString(), logEvent.Timestamp);

        if (entry.GetByteSize() > _logEventLimitBytes)
        {
            SelfLog.WriteLine(
                "Log event exceeds the size limit of {0} bytes set for this sink and will be dropped; data: {1}",
                _logEventLimitBytes,
                JsonSerializer.Serialize(entry));
            return;
        }

        // Add LogEvent Labels
        entry.Labels.Add(GrafanaLokiHelpers.LogLevelLabelName, logEvent.Level.ToGrafanaString());
        foreach (var property in logEvent.Properties)
        {
            // Some enrichers pass strings with quotes surrounding the values inside the string,
            // which results in redundant quotes after serialization and a "bad request" response.
            // To avoid this, replace all quotes from the value.
            entry.Labels.AddOrReplace(property.Key, property.Value.ToString().Replace("\"", _propertiesDelimiter ?? "`"));
        }

        var result = _queue.TryEnqueue(entry);
        if (result == LogEventsQueue.EnqueueResult.QueueFull)
        {
            SelfLog.WriteLine("Queue has reached its limit and the log event will be dropped; data: {0}", JsonSerializer.Serialize(entry));
        }
    }

    public void Dispose()
    {
        lock (_syncRoot)
        {
            if (_disposed)
                return;

            _disposed = true;
        }

        _timer.Dispose();

        OnTick().GetAwaiter().GetResult();
        _httpClient.Dispose();
    }

    private void SetTimer()
    {
        // Note, called under syncRoot
        _timer.Start(_connectionSchedule.NextInterval);
    }

    private async Task OnTick()
    {
        try
        {
            LogEventsBatch? batch;

            do
            {
                batch = _unsentBatch ?? LogEventsQueueReader.Read(_queue, _logEventsInBatchLimit, _batchSizeLimitBytes);

                if (batch.LogEvents.Count > 0)
                {
                    HttpResponseMessage response;

                    using (var contentStream = new MemoryStream())
                    using (var contentWriter = new StreamWriter(contentStream, Encoding.UTF8WithoutBom))
                    {
                        _batchFormatter.Format(batch.LogEvents, contentWriter);

                        await contentWriter.FlushAsync();
                        contentStream.Position = 0;

                        if (contentStream.Length == 0)
                            continue;

                        response = await _httpClient
                            .PostAsync(_requestUri, contentStream)
                            .ConfigureAwait(false);
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        _connectionSchedule.MarkSuccess();
                        _unsentBatch = null;
                    }
                    else
                    {
                        _connectionSchedule.MarkFailure();
                        _unsentBatch = batch;

                        var statusCode = response.StatusCode;
                        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        SelfLog.WriteLine("Received failed HTTP shipping result {0}: {1}", statusCode, body);
                        break;
                    }
                }
                else
                {
                    // For whatever reason, there's nothing waiting to be sent. This means we should try connecting
                    // again at the regular interval, so mark the attempt as successful.
                    _connectionSchedule.MarkSuccess();
                }
            } while (batch.HasReachedLimit);
        }
        catch (Exception e)
        {
            SelfLog.WriteLine("Exception while emitting periodic batch from {0}: {1}", this, e);
            _connectionSchedule.MarkFailure();
        }
        finally
        {
            lock (_syncRoot)
            {
                if (!_disposed)
                {
                    SetTimer();
                }
            }
        }
    }
}