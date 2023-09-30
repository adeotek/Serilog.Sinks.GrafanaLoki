using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Serilog.Debugging;
using Serilog.Sinks.GrafanaLoki.Common;

namespace Serilog.Sinks.GrafanaLoki;

public class GrafanaLokiHttpClient : IHttpClient
{
    private const string JsonContentType = "application/json";
    protected readonly HttpClient HttpClient;

    public GrafanaLokiHttpClient(HttpClient? httpClient = null, GrafanaLokiCredentials? credentials = null, int httpTimeout = -1)
    {
        HttpClient = httpClient ?? new HttpClient();
        if (httpTimeout > 0)
        {
            HttpClient.Timeout = new TimeSpan(0,0,0,0, httpTimeout);
        }

        SetCredentials(credentials);
    }

    ~GrafanaLokiHttpClient()
    {
        Dispose(false);
    }

    /// <inheritdoc />
    public bool DebugMode { get; set; }

    /// <inheritdoc />
    public void SetCredentials(GrafanaLokiCredentials? credentials)
    {
        if (credentials == null || string.IsNullOrEmpty(credentials.User))
        {
            return;
        }
        var headers = HttpClient.DefaultRequestHeaders;
        if (headers.Any(x => x.Key == "Authorization"))
        {
            return;
        }

        var token = Helpers.Base64Encode($"{credentials.User}:{credentials.Password}");
        headers.Add("Authorization", $"Basic {token}");
    }

    /// <inheritdoc />
    public virtual async Task<HttpResponseMessage> PostAsync(string requestUri, Stream contentStream)
    {
        try
        {
            using var content = new StreamContent(contentStream);
            content.Headers.Add("Content-Type", JsonContentType);

            var response = await HttpClient
                .PostAsync(requestUri, content)
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var body = await response.Content.ReadAsStringAsync();
                if (DebugMode)
                {
                    SelfLog.WriteLine("GrafanaLoki sending data: {0}", Helpers.StreamToString(contentStream));
                    SelfLog.WriteLine("GrafanaLoki response StatusCode: {0} - {1}", (int)response.StatusCode, response.ReasonPhrase);
                    SelfLog.WriteLine("GrafanaLoki response body: {0}", body);
                }

                if (body.Contains("error parsing labels:") || body.Contains("ignored, reason: 'entry out of order' for stream"))
                {
                    SelfLog.WriteLine("Bad request Loki response: {0}", body);
                    response.StatusCode = HttpStatusCode.OK;
                    if (DebugMode)
                    {
                        SelfLog.WriteLine("GrafanaLoki error suppressed!!!");
                    }
                }
            }
            else if (DebugMode)
            {
                SelfLog.WriteLine("GrafanaLoki sending data: {0}", Helpers.StreamToString(contentStream));
                SelfLog.WriteLine("GrafanaLoki response StatusCode: {0} - {1}", (int)response.StatusCode, response.ReasonPhrase);
                if (response.StatusCode != HttpStatusCode.NoContent && response.StatusCode != HttpStatusCode.OK)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    SelfLog.WriteLine("GrafanaLoki response body: {0}", body);
                }
            }

            return response;
        }
        catch (Exception ex)
        {
            if (DebugMode)
            {
                SelfLog.WriteLine("GrafanaLoki POST exception: {0}", ex.Message);
            }
            throw;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            HttpClient.Dispose();
        }
    }
}