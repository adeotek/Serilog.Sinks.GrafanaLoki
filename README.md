# Serilog.Sinks.GrafanaLoki
A Serilog Sink for Grafana's [Loki Log Aggregator](https://grafana.com/loki).

![.NET Core](https://github.com/adeotek/Serilog.Sinks.GrafanaLoki/workflows/.NET%20Core/badge.svg?branch=main)

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
                    "debugMode": true
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
