using System;
using System.Collections.Generic;

namespace Serilog.Sinks.GrafanaLoki
{
    public static class Helpers
    {
        public const string DefaultOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} | [{Level:u3}] | {Message:lj} | {Exception}";
        public const string PostDataUri = "/loki/api/{0}/push";
        public const string DefaultApiVersion = "v1";

        public static string BuildPostUri(string url, string apiVersion = null)
        {
            return string.Format("{0}{1}", url.TrimEnd('/'), string.Format(PostDataUri, apiVersion ?? DefaultApiVersion));
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        public static string GetUnixTimestamp(DateTime? dateTime = null)
        {
            var localTime = dateTime ?? DateTime.Now;
            var localTimeAndOffset = new DateTimeOffset(localTime, TimeZoneInfo.Local.GetUtcOffset(localTime));
            var time = localTimeAndOffset.ToUnixTimeMilliseconds() * 1000000;
            return time.ToString();
        }

        public static Dictionary<string, string> AddOrReplace(this Dictionary<string, string> obj, string key, string value)
        {
            if (obj.ContainsKey(key))
            {
                obj[key] = value;
            }
            else
            {
                obj.Add(key, value);
            }
            return obj;
        }

        public static Dictionary<string, string> AddOrAppend(this Dictionary<string, string> obj, string key, string value)
        {
            if (obj.ContainsKey(key))
            {
                obj[key] = (obj[key] ?? string.Empty) + (value ?? string.Empty);
            }
            else
            {
                obj.Add(key, value);
            }
            return obj;
        }
    }
}
