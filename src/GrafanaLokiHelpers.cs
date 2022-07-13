namespace Serilog.Sinks.GrafanaLoki;

public static class GrafanaLokiHelpers
{
    public const string LogLevelLabelName = "level";
    public const string DefaultOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} | [{Level:u3}] | {Message:lj} | {Exception}";
    public const string PostDataUri = "/loki/api/{0}/push";
    public const string DefaultApiVersion = "v1";

    public static string BuildPostUri(string url, string? apiVersion = null)
        => $"{url.TrimEnd('/')}{string.Format(PostDataUri, apiVersion ?? DefaultApiVersion)}";
}