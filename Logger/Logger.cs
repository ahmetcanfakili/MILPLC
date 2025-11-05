using System;
using Serilog;
using Serilog.Events;

namespace MILPLC.Logger
{
    public static class LoggerConfig
    {
        public static void InitializeLogger()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()

                .WriteTo.File(
                    path: "logs/MILPLC-log-.txt",
                    restrictedToMinimumLevel: LogEventLevel.Information,
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")

                .WriteTo.Console(
                    restrictedToMinimumLevel: LogEventLevel.Debug,
                    outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")

                .CreateLogger();

            Log.Information("Serilog başlatıldı. Uygulama logları izlenmeye başlandı.");
        }

        public static void CloseLogger()
        {
            Log.Information("Uygulama kapatılıyor. Logger sonlandırılıyor.");
            Log.CloseAndFlush();
        }
    }
}