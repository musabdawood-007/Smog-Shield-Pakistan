using System.Windows;
using System.Windows.Threading;

namespace SmogShield_Pakistan
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            Services.WorldClock.Start();
            System.Threading.Tasks.Task.Run(() => Services.DatabaseHelper.Initialize());
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Error: {e.Exception.Message}\n\n{e.Exception.StackTrace}",
                "SmogShield Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
