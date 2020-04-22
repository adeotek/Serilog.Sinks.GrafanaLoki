using System.Collections.Generic;
using Newtonsoft.Json;

namespace Serilog.Sinks.GrafanaLoki
{
    internal class GrafanaLokiStream
    {
        [JsonProperty("stream")]
        public Dictionary<string, string> Labels { get; } = new Dictionary<string, string>();

        [JsonIgnore]
        public Dictionary<string, string> Entries { get; } = new Dictionary<string, string>();

        [JsonProperty("values")]
        public List<List<string>> Values
        {
            get
            {
                var result = new List<List<string>>();
                foreach (var item in Entries)
                {
                    result.Add(new List<string>() { item.Key, item.Value });
                }
                return result;
            }
        }
    }
}
