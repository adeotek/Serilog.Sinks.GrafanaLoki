namespace Serilog.Sinks.GrafanaLoki.Sample;

public class CustomHttpClient : GrafanaLokiHttpClient
{
    public override async Task<HttpResponseMessage> PostAsync(string requestUri, Stream contentStream)
    {
        using var content = new StreamContent(contentStream);
        content.Headers.Add("Content-Type", "application/json");

        var response = await HttpClient
            .PostAsync(requestUri, content)
            .ConfigureAwait(false);

        Console.WriteLine($"StatusCode: {(int)response.StatusCode} - {response.ReasonPhrase}");
        var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        Console.WriteLine($"Body: {body}");

        return response;
    }
}