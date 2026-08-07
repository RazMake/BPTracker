using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace BPTracker.Desktop;

// Our own BPTracker.Application namespace shadows System.Windows.Application,
// so the base type has to be fully qualified.
public partial class App : System.Windows.Application
{
    private ServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            _services = DesktopServices.Build();

            var window = _services.GetRequiredService<MainWindow>();
            MainWindow = window;
            window.Show();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"BPTracker could not start.\n\n{exception.Message}",
                "BPTracker",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        base.OnExit(e);
    }
}
