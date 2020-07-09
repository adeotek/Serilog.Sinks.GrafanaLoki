using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Serilog.Context;

namespace Serilog.Sinks.GrafanaLoki.Example
{
    class Program
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
                Log.Error(ex, "Exeption caught");
            }

            using (LogContext.PushProperty("MyProperty", 16))
            {
                Log.Warning("Warning with MyProperty");
                Log.Fatal("Fatal with MyProperty");
            }

            Log.CloseAndFlush();

            Console.ReadKey(true);
        }

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
                    new List<GrafanaLokiLabel>() { new GrafanaLokiLabel() { Key = "app", Value = "Serilog.Sinks.GrafanaLoki.Example" } },
                    Events.LogEventLevel.Debug,
                    Helpers.DefaultOutputTemplate,
                    null,
                    null,
                    1000,
                    null,
                    null,
                    null,
                    new CustomHttpClient()
                )
                .CreateLogger();
        }
    }
}
