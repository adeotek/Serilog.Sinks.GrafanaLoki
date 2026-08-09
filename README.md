# Serilog.Sinks.GrafanaLoki
A Serilog Sink for Grafana's [Loki Log Aggregator](https://grafana.com/loki).

[![.NET build](https://github.com/adeotek/Serilog.Sinks.GrafanaLoki/actions/workflows/dotnet_build.yml/badge.svg)](https://github.com/adeotek/Serilog.Sinks.GrafanaLoki/actions/workflows/dotnet_build.yml)

What is Loki?

> Loki is a horizontally-scalable, highly-available, multi-tenant log aggregation system inspired by Prometheus. It is designed to be very cost effective and easy to operate, as it does not index the contents of the logs, but rather a set of labels for each log stream.

You can find more information about what Loki is over on [Grafana's website here](https://grafana.com/loki).


## Features:

- Timestamps precision at 100ns (lower risk of collision between log entries)
- Uses the new Loki HTTP API
- Serilog.Settings.Configuration integration (configure sink via configuration file, JSON sample provided in Example project)
- Global and contextual labels support
- Log entries are grouped in Streams by log level and other contextual labels
- Logs are send to Loki in batches via HTTP using internal client
- Customizable HTTP client


## Installation

The Serilog.Sinks.GrafanaLoki NuGet [package can be found here](https://www.nuget.org/packages/Serilog.Sinks.GrafanaLoki/). Alternatively you can install it via one of the following commands below:

NuGet command:
```bash
Install-Package Serilog.Sinks.GrafanaLoki
```
.NET Core CLI:
```bash
dotnet add package Serilog.Sinks.GrafanaLoki
```


## Basic Example:

```csharp
var credentials = new GrafanaLokiCredentials()
{
    User = "<username>",
    Password = "<password>"
};

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ALabel", "ALabelValue")
    .WriteTo.GrafanaLoki(
        "http://localhost:3100",
        credentials,
        new Dictionary<string, string>() { { "app", "Serilog.Sinks.GrafanaLoki.Sample" } }, // Global labels
        Events.LogEventLevel.Debug
    )
    .CreateLogger();

Log.Information("Logs are now sent to Loki at address {@Url}.", "http://localhost:3100");

Log.CloseAndFlush();
```


### Adding contextual (local) labels

If you need to add contextual labels from a particular class or method, you can achieve this with the following code:

```csharp
using (LogContext.PushProperty("ALabel", "ALabelValue"))
{
    log.Information("Info with ALabel");
    log.Warning("Warning with ALabel");
}
```


### Exception Labels

When a log event includes an exception, two optional labels can be attached to the Loki stream:

- **`exception_type`** — the fully-qualified exception type name (e.g. `System.InvalidOperationException`).  
  This is **enabled by default** because it has low cardinality (a finite set of exception types per application) and is safe for Loki label indexing.

- **`exception`** — the full `Exception.ToString()` output (message + stack trace).  
  This is **disabled by default** because it can introduce high label cardinality — every unique exception message and stack trace creates a new label value, which can degrade Loki query performance and increase storage cost. Enable only if you need to query by exception text directly in Loki and understand the cardinality trade-off.

```csharp
.WriteTo.GrafanaLoki(
    "http://localhost:3100",
    exceptionTypeAsLabel: true,   // default: true
    exceptionAsLabel: false       // default: false
)
```

Both flags can also be configured via `appsettings.json` (see the configuration sample below).


### Structured Metadata (Loki 3.0+)

Loki 3.0 introduced [structured metadata](https://grafana.com/docs/loki/latest/get-started/labels/structured-metadata/) — a way to attach metadata to log lines without indexing them as labels. This is recommended for high-cardinality properties that should not be used as labels.

When `useStructuredMetadata` is enabled, all log event properties are sent as structured metadata instead of labels. The `level`, `exception_type`, and `exception` labels (if enabled) remain as labels. Global labels also remain as labels.

```csharp
.WriteTo.GrafanaLoki(
    "http://localhost:3100",
    useStructuredMetadata: true   // default: false
)
```

Structured metadata can also be configured via `appsettings.json`.

> **Note:** Structured metadata requires Loki 3.0+ with `allow_structured_metadata: true` and schema version `v13` or higher.


### Label Limit Safeguard

Loki 3.0 defaults to a maximum of 15 labels per series (down from 30). Use `maxLabelCount` to enforce a label limit and prevent Loki from rejecting log entries with too many labels.

When the label count exceeds the limit, excess property labels are dropped with a `SelfLog` warning. If `useStructuredMetadata` is also enabled, excess labels are moved to structured metadata instead of being dropped.

```csharp
.WriteTo.GrafanaLoki(
    "http://localhost:3100",
    maxLabelCount: 15,             // default: null (no limit)
    useStructuredMetadata: true    // recommended: excess labels moved to metadata
)
```


### Gzip Compression

Enable gzip compression to reduce bandwidth when sending log data to Loki. Loki supports `Content-Encoding: gzip` on the push endpoint.

```csharp
.WriteTo.GrafanaLoki(
    "http://localhost:3100",
    useGzipCompression: true   // default: false
)
```

> **Note:** Gzip compression applies only to the default `GrafanaLokiHttpClient`. Custom `IHttpClient` implementations must handle compression themselves.


### Custom HTTP Client

Serilog.Loki.GrafanaLoki uses by default the internal HTTP Client, but you can customize it by implementing the `Serilog.Sinks.GrafanaLoki.Common.IHttpClient` interface or by extending the `Serilog.Sinks.GrafanaLoki.GrafanaLokiHttpClient` class.

```csharp
// CustomHttpClient.cs

public class CustomHttpClient : GrafanaLokiHttpClient
{
    public override async Task<HttpResponseMessage> PostAsync(string requestUri, Stream contentStream)
    {
        using var content = new StreamContent(contentStream);
        content.Headers.Add("Content-Type", "application/json");
        var response = await HttpClient
            .PostAsync(requestUri, content)
            .ConfigureAwait(false);
        return response;
    }
}
```
```csharp
// Usage

var credentials = new GrafanaLokiCredentials()
{
    User = "<username>",
    Password = "<password>"
};

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ALabel", "ALabelValue")
    .WriteTo.GrafanaLoki(
        url: "http://localhost:3100",
        credentials: credentials,
        httpClient: new CustomHttpClient()
    )
    .CreateLogger();
```


### Using application settings configuration (`appsettings.json`)

In order to configure this sink using _Microsoft.Extensions.Configuration_, for example with ASP.NET Core or .NET Core, the package has as dependency the [Serilog.Settings.Configuration](https://github.com/serilog/serilog-settings-configuration) package.
This example is for the JSON configuration file, but it should work fine with any configuration source (.ini, XML etc.) by making the appropriate format changes.

Instead of configuring the sink directly in code, you can make all the configurations in the configuration file and then just call `ReadFrom.Configuration()` method:

```csharp
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();
```

`appsettings.json` configuration sample:
```json
{
    "Serilog": {
        "WriteTo": [
            {
                "Name": "GrafanaLoki",
                "Args": {
                    "Url": "http://localhost:3100",
                    "Credentials": {
                        "User": "<username>",
                        "Password": "<password>"
                    },
                    "Labels": {
                        "project": "Serilog.Sinks.GrafanaLoki",
                        "app": "Serilog.Sinks.GrafanaLoki.Sample"
                    },
                    "restrictedToMinimumLevel": "Debug",
                    "outputTemplate": "{Timestamp:HH:mm:ss} [{Level:u3}] | {Message:lj} | {Exception:1}",
                    "propertiesStringDelimiter": null,
                    "logEventsInBatchLimit": 1000,
                    "queueLimitBytes": null,
                    "logEventLimitBytes": null,
                    "period": null,
                    "httpRequestTimeout": 3000,
                    "debugMode": true,
                    "exceptionTypeAsLabel": true,
                    "exceptionAsLabel": false
                }
            }
        ]
    }
}
```
Excepting the ``Url``, all configuration items are optional.


### Inspiration and Credits
- [Serilog.Sinks.Loki](https://github.com/JosephWoodward/Serilog-Sinks-Loki)
- [Serilog.Sinks.Http](https://github.com/FantasticFiasco/serilog-sinks-http)
