using System.Configuration;
using System.Data;
using System.Windows;
using System.Threading;
using System.Runtime.Remoting;
using System.Security.Principal;
using MILPLC.Logger;
using static Serilog.Log;

namespace MILPLC
{
    public partial class App : Application
    {
        private const string AppMutexName = "Global\\MILPLC_SingleInstance_Mutex_{USER_SID}";
        private static Mutex _mutex;
        private static bool _isAnotherInstanceRunning = false;

        protected override void OnStartup(StartupEventArgs e)
        {
            LoggerConfig.InitializeLogger();
            Information("MILPLC Starting...");

            // Create a unique mutex name per user
            string userSid = GetUserSid();
            string mutexName = AppMutexName.Replace("{USER_SID}", userSid);

            try
            {
                _mutex = new Mutex(true, mutexName, out bool createdNew);

                if (!createdNew)
                {
                    _isAnotherInstanceRunning = true;
                    Information("Another instance of MILPLC is already running. Shutting down...");

                    ShowAlreadyRunningMessage();

                    _mutex?.Close();
                    Current.Shutdown();
                    return;
                }

                Information("MILPLC started successfully - No other instances detected.");

                // Set up global exception handling
                SetupExceptionHandling();

                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                Error(ex, "Error while checking for existing instances");
                MessageBox.Show("Error starting application: " + ex.Message,
                              "Startup Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Information($"MILPLC shutting down. Exit code: {e.ApplicationExitCode}");

            // Release and close the mutex
            try
            {
                if (_mutex != null && !_isAnotherInstanceRunning)
                {
                    _mutex.ReleaseMutex();
                    _mutex.Close();
                }
                _mutex = null;
            }
            catch (Exception ex)
            {
                Error(ex, "Error while releasing mutex");
            }

            LoggerConfig.CloseLogger();
            base.OnExit(e);
        }

        private string GetUserSid()
        {
            // Get current user's SID for mutex name uniqueness
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                return identity.User?.Value ?? "DefaultUser";
            }
            catch
            {
                return "UnknownUser";
            }
        }

        private void ShowAlreadyRunningMessage()
        {
            MessageBox.Show(
                "MILPLC is already running!\n\n" +
                "Only one instance of the application can be run at a time.\n\n" +
                "Please check your system tray or taskbar for the running instance.",
                "Application Already Running",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private void SetupExceptionHandling()
        {
            // Global exception handlers
            this.DispatcherUnhandledException += (sender, e) =>
            {
                Error(e.Exception, "Unhandled UI exception occurred!");
                MessageBox.Show(
                    $"An unexpected error occurred:\n{e.Exception.Message}",
                    "Unexpected Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                e.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var exception = e.ExceptionObject as Exception;
                Fatal(exception, "Unhandled application domain exception! Application will exit.");

                MessageBox.Show(
                    $"A critical error occurred and the application must close:\n{exception?.Message}",
                    "Critical Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            };

            // Task scheduler exceptions
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                Error(e.Exception, "Unobserved task exception occurred!");
                e.SetObserved();
            };
        }
    }
}