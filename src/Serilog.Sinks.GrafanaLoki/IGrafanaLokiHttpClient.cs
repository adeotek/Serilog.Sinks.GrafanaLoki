using Serilog.Sinks.Http;

namespace Serilog.Sinks.GrafanaLoki
{
    /// <summary>
    /// Interface responsible for posting HTTP requests.
    /// </summary>
    public interface IGrafanaLokiHttpClient : IHttpClient
    {
        /// <summary>
        /// Set basic authentification credentials.
        /// </summary>
        /// <param name="credentials">The GrafanaLokiCredentials instance.</param>
        void SetCredentials(GrafanaLokiCredentials credentials);
    }
}
