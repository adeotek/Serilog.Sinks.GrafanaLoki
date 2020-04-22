using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Serilog.Sinks.GrafanaLoki
{
    internal class GrafanaLokiBatch
    {
        [JsonProperty("streams")]
        public List<GrafanaLokiStream> Streams { get; set; } = new List<GrafanaLokiStream>();

        public string Serialize()
        {
            JsonSerializer serializer = new JsonSerializer();
            TextWriter writer = new StringWriter();
            serializer.Serialize(writer, this);
            return writer.ToString();
        }
    }
}
