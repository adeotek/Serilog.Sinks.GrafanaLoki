using Serilog;
using Serilog.Context;
using Serilog.Sinks.GrafanaLoki.Sample;

// Create Logger from ConfigurationManager (appsettings.json)
LoggerSetup.SetLoggerFromConfiguration();

//// Create Logger programatically
//LoggerSetup.SetLoggerProgrammatically();


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
