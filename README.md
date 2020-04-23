# Serilog.Sinks.GrafanaLoki
A Serilog Sink for Grafana's [Loki Log Aggregator](https://grafana.com/loki).

What is Loki?

> Loki is a horizontally-scalable, highly-available, multi-tenant log aggregation system inspired by Prometheus. It is designed to be very cost effective and easy to operate, as it does not index the contents of the logs, but rather a set of labels for each log stream.

You can find more information about what Loki is over on [Grafana's website here](https://grafana.com/loki).


## Features:

- Uses the new Loki HTTP API
- Serilog.Settings.Configuration integration (configure sink via configuration file, JSON sample provided in Example project)
- Global and contextual labels support 
- Logs are send to Loki via HTTP using Serilog.Sinks.Http package (batch support included)
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
        new List<GrafanaLokiLabel>() { new GrafanaLokiLabel() { Key = "app", Value = "Serilog.Sinks.GrafanaLoki.Example" } }, // Global labels
        Events.LogEventLevel.Debug
    )
    .CreateLogger();

Log.Information("Logs are now sent to Loki at address {@Url}.", "http://localhost:3100");

Log.CloseAndFlush();
```


### Adding contextual (local) labels

If you need to add contextual labels from a particular class or method, you can achive this with the following code:

```csharp
using (LogContext.PushProperty("ALabel", "ALabelValue"))
{
    log.Information("Info with ALabel");
    log.Warning("Warning with ALabel");
}
```


### Custom HTTP Client

Serilog.Loki.GrafanaLoki is built on top of the popular [Serilog.Sinks.Http](https://github.com/FantasticFiasco/serilog-sinks-http) library to post log entries to Loki.
In order to use a custom HttpClient you can extend the default HttpClient (`Serilog.Sinks.GrafanaLoki.GrafanaLokiHttpClient`), or create one from scratch by implementing `Serilog.Sinks.Http.IHttpClient`.

```csharp
// CustomHttpClient.cs

public class CustomHttpClient : GrafanaLokiHttpClient
{
    public override async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content)
    {
        var req = content.ReadAsStringAsync().Result;
        var response = await base.PostAsync(requestUri, content);
        var body = response.Content.ReadAsStringAsync().Result;
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

In order tu configure this sink using _Microsoft.Extensions.Configuration_, for example with ASP.NET Core or .NET Core, the package has as dependecy the [Serilog.Settings.Configuration](https://github.com/serilog/serilog-settings-configuration) package.
This example is for the JSON configuration file, but it should work fine with any configuration source (.ini, XML etc.) by making the apropriate format changes.

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
            "User": "",
            "Password": ""
          },
          "restrictedToMinimumLevel": "Debug",
          "outputTemplate": "{Timestamp:HH:mm:ss} | {Level:u3} | {Message:lj} | {Exception:1}",
          "Labels": [
            {

              "Key": "project",
              "Value": "Serilog.Sinks.GrafanaLoki"
            },
            {
              "Key": "app",
              "value": "Serilog.Sinks.GrafanaLoki.Example"
            }
          ],
          "batchPostingLimit": 1000,
          "queueLimit": null,
          "period": null
        }
      }
    ]
  }
}
```
Excepting the ``Url``, all configuration options are optional.


### Inspiration and Credits
- [Serilog.Sinks.Loki](https://github.com/JosephWoodward/Serilog-Sinks-Loki)
- [Serilog.Sinks.Http](https://github.com/FantasticFiasco/serilog-sinks-http)