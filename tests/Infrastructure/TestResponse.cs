namespace Serilog.Sinks.GrafanaLoki.Tests.Infrastructure;

public class TestResponse
{
    public IList<LokiStream> Streams { get; set; } = new List<LokiStream>();
}

public class LokiStream
{
    public Dictionary<string, string> Labels { get; set; } = new();

    public IList<IList<string>> Values { get; set; } = new List<IList<string>>();
}