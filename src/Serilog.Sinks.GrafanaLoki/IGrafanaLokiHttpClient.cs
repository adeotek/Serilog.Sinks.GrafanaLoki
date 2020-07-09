using Serilog.Sinks.Http;

namespace Serilog.Sinks.GrafanaLoki
{
    /// <summary>
    /// Interface responsible for posting HTTP requests.
    /// </summary>
    public interface IGrafanaLokiHttpClient : IHttpClient
    {
        /// <summary>
        /// Set basic authentication credentials.
        /// </summary>
        /// <param name="credentials">The GrafanaLokiCredentials instance.</param>
        void SetCredentials(GrafanaLokiCredentials credentials);

        /// <summary>
        /// DebugMode property.
        /// </summary>
        bool DebugMode { get; set; }
    }
}
