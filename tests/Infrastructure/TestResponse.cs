namespace Serilog.Sinks.GrafanaLoki.Tests.Infrastructure;

public class TestResponse
{
    public TestResponse()
    {
        Streams = new List<Stream>();
    }

    public IList<Stream> Streams { get; set; }
}

public class Stream
{
    public Dictionary<string, string> Labels { get; set; }

    public List<List<string>> Values { get; set; }
}