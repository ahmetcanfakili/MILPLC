using System;
using Serilog;
using Serilog.Events;

namespace MILPLC.Logger
{
    public static class LoggerConfig
    {
        public static void InitializeLogger()
        {
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.File(
                    path: "logs/MILPLC-log-.txt", 
                    restrictedToMinimumLevel: LogEventLevel.Information,
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.Console(
                    restrictedToMinimumLevel: LogEventLevel.Debug,
                    outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
            Log.Information("Serilog initialized. Application logs are now being monitored.");
        }
        
        public static void CloseLogger()
        {
            Log.Information("Application is shutting down. The logger is being terminated.");
            Log.CloseAndFlush();
        }
    }
}
