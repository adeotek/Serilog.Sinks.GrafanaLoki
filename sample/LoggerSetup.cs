using Microsoft.Extensions.Configuration;

namespace Serilog.Sinks.GrafanaLoki.Sample;

internal static class LoggerSetup
{
    public static void SetLoggerFromConfiguration()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(config)
            .CreateLogger();
    }

    public static void SetLoggerProgrammatically()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("MyPropertyName", "MyPropertyValue")
            .WriteTo.Console()
            .WriteTo.GrafanaLoki(
                url: "http://localhost:3100",
                credentials: null,
                labels: new Dictionary<string, string>
                {
                    { "Environment", "Sample" },
                    { "Application", "Serilog.Sinks.GrafanaLoki.Sample" }
                },
                restrictedToMinimumLevel: Events.LogEventLevel.Debug,
                outputTemplate: GrafanaLokiHelpers.DefaultOutputTemplate,
                propertiesStringDelimiter: null,
                formatProvider: null,
                batchFormatter: null,
                queueLimitBytes: null,
                logEventLimitBytes: 1000,
                logEventsInBatchLimit: null,
                batchSizeLimitBytes: null,
                period: TimeSpan.FromSeconds(2),
                apiVersion: null,
                httpClient: new CustomHttpClient(),
                httpRequestTimeout: 3000,
                debugMode: false,
                exceptionTypeAsLabel: true,
                exceptionAsLabel: false
            )
            .CreateLogger();
    }
}