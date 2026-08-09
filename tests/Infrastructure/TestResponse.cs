namespace Serilog.Sinks.GrafanaLoki.Tests.Infrastructure;

public class TestResponse
{
    public IList<Stream> Streams { get; set; } = new List<Stream>();
}

public class Stream
{
    public Dictionary<string, string> Labels { get; set; }

    public List<List<string>> Values { get; set; }
}