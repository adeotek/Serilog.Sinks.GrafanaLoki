using Microsoft.Extensions.Configuration;
using Serilog.Context;
using Serilog.Sinks.GrafanaLoki.Common;

namespace Serilog.Sinks.GrafanaLoki.Sample;

static class Program
{
    static void Main(string[] args)
    {
        //// Create Logger from ConfigurationManager (appsettings.json)
        SetLoggerFromConfiguration();

        //// Create Logger programatically
        //SetLoggerProgrammatically();


        Log.Information("Logger started!");

        var totalItems = 5;
        for (var i = 0; i < totalItems; i++)
        {
            Log.Debug("Item {ItemIndex} of {TotalItems}", i + 1, totalItems);
            Thread.Sleep(1000);
        }

        try
        {
            string r = (string)new object();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception caught");
        }

        using (LogContext.PushProperty("MyProperty", 16))
        {
            Log.Warning("Warning with MyProperty");
            Log.Fatal("Fatal with MyProperty");
        }

        Log.CloseAndFlush();

        Console.ReadKey(true);
    }

    private static void SetLoggerFromConfiguration()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(config)
            .CreateLogger();
    }

    private static void SetLoggerProgrammatically()
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