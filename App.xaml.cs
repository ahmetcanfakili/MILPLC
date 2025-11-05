using System.Configuration;
using System.Data;
using System.Windows;
using MILPLC.Logger;
using static Serilog.Log;

namespace MILPLC
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            LoggerConfig.InitializeLogger();
            Information("MILPLC Starting...");
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            LoggerConfig.CloseLogger();
            base.OnExit(e);
        }
    }
}