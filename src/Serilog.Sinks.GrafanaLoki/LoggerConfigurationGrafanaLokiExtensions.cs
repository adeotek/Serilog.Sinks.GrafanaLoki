﻿using System;
using System.Collections.Generic;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using Serilog.Sinks.Http;

namespace Serilog.Sinks.GrafanaLoki
{
    /// <summary>Extends <see cref="LoggerConfiguration"/> with methods to add file sinks.</summary>
    public static class LoggerConfigurationGrafanaLokiExtensions
    {
        /// <summary>
        /// Send log events to Grafana's Loki.
        /// </summary>
        /// <param name="sinkConfiguration">Logger sink configuration.</param>
        /// <param name="url">Base URL for Loki API.</param>
        /// <param name="credentials">Loki Http credentials.</param>
        /// <param name="labelsProvider">Log events labels provider.</param>
        /// <param name="restrictedToMinimumLevel">
        /// The minimum level for events passed through the sink. Ignored when <paramref name="levelSwitch"/> is specified.
        /// </param>
        /// <param name="outputTemplate">
        /// A message template describing the format used to write to the sink.
        /// The default is "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}".
        /// </param>
        /// <param name="formatProvider">
        /// Supplies culture-specific formatting information, or null.
        /// </param>
        /// <param name="batchFormatter">
        /// The formatter batching multiple log events into a payload that can be sent over the
        /// network. Default value is <see cref="DefaultBatchFormatter"/>.
        /// </param>
        /// <param name="batchPostingLimit">
        /// The maximum number of events to post in a single batch. Default value is 1000.
        /// </param>
        /// <param name="queueLimit">
        /// The maximum number of events stored in the queue in memory, waiting to be posted over
        /// the network. Default value is infinitely.
        /// </param>
        /// <param name="period">
        /// The time to wait between checking for event batches. Default value is 2 seconds.
        /// </param>
        /// <param name="apiVersion">Loki API version, default "v1".</param>
        /// <param name="httpClient">Custom HttpClient or null or default (IHttpClient).</param>
        /// <param name="httpRequestTimeout">HttpRequest timeout where null is the default HttpClient timeout (default null).</param>
        /// <param name="debugMode">Debug mod switch on/off.</param>
        /// <returns>Configuration object allowing method chaining.</returns>
        public static LoggerConfiguration GrafanaLoki(
            this LoggerSinkConfiguration sinkConfiguration,
            string url,
            GrafanaLokiCredentials credentials = null,
            IEnumerable<GrafanaLokiLabel> labels = null,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            string outputTemplate = Helpers.DefaultOutputTemplate,
            IFormatProvider formatProvider = null,
            IBatchFormatter batchFormatter = null,
            int batchPostingLimit = 1000,
            int? queueLimit = null,
            TimeSpan? period = null,
            string apiVersion = null,
            IGrafanaLokiHttpClient httpClient = null,
            int? httpRequestTimeout = null,
            bool debugMode = false)
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

            var formatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);
            return ConfigureGrafanaLoki(sinkConfiguration, url, credentials, labels, restrictedToMinimumLevel, formatter, batchFormatter, batchPostingLimit, queueLimit, period, apiVersion, httpClient, httpRequestTimeout, debugMode);
        }

        private static LoggerConfiguration ConfigureGrafanaLoki(
            this LoggerSinkConfiguration sinkConfiguration,
            string url,
            GrafanaLokiCredentials credentials,
            IEnumerable<GrafanaLokiLabel> labels,
            LogEventLevel restrictedToMinimumLevel,
            ITextFormatter formatter,
            IBatchFormatter batchFormatter,
            int batchPostingLimit,
            int? queueLimit,
            TimeSpan? period,
            string apiVersion,
            IGrafanaLokiHttpClient httpClient,
            int? httpRequestTimeout,
            bool debugMode)
        {
            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            if (httpClient == null)
            {
                httpClient = new GrafanaLokiHttpClient(null, credentials, httpRequestTimeout ?? -1);
            }
            else if (credentials != null)
            {
                httpClient.SetCredentials(credentials);
            }
            httpClient.DebugMode = debugMode;
            if (batchFormatter == null)
            {
                batchFormatter = labels == null ? new GrafanaLokiBatchFormatter() : new GrafanaLokiBatchFormatter(labels);
            }
            var requestUri = Helpers.BuildPostUri(url, apiVersion);

            return sinkConfiguration.Http(requestUri, batchPostingLimit, queueLimit, period, formatter, batchFormatter, restrictedToMinimumLevel, httpClient);
        }
    }
}
