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
                "http://localhost:3100",
                null,
                new Dictionary<string, string>
                {
                    { "Environment", "Sample" },
                    { "Application", "Serilog.Sinks.GrafanaLoki.Sample" }
                },
                Events.LogEventLevel.Debug,
                GrafanaLokiHelpers.DefaultOutputTemplate,
                null,
                null,
                null,
                null,
                1000,
                null,
                null,
                TimeSpan.FromSeconds(2),
                null,
                new CustomHttpClient(),
                3000
            )
            .CreateLogger();
    }
}