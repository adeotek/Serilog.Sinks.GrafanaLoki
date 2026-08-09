namespace Serilog.Sinks.GrafanaLoki.Tests.Infrastructure;

public class TestResponse
{
    public IList<Stream> Streams { get; set; } = new List<Stream>();
}

public class Stream
{
    public Dictionary<string, string> Labels { get; set; } = new();

    public IList<IList<string>> Values { get; set; } = new List<IList<string>>();
}