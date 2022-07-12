using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Serilog.Sinks.GrafanaLoki.Common;

/// <summary>
/// Interface responsible for posting HTTP requests.
/// </summary>
public interface IHttpClient : IDisposable
{
    /// <summary>
    /// DebugMode property.
    /// </summary>
    bool DebugMode { get; set; }

    /// <summary>
    /// Set basic authentication credentials.
    /// </summary>
    /// <param name="grafanaLokiCredentials">The GrafanaLokiCredentials instance.</param>
    void SetCredentials(GrafanaLokiCredentials grafanaLokiCredentials);

    /// <summary>
    /// Sends a POST request to the specified Uri as an asynchronous operation.
    /// </summary>
    /// <param name="requestUri">The Uri the request is sent to.</param>
    /// <param name="contentStream">The stream containing the content of the request.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    Task<HttpResponseMessage> PostAsync(string requestUri, Stream contentStream);
}