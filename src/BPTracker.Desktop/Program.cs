using Velopack;

namespace BPTracker.Desktop;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        // Must be the very first thing that runs: Velopack uses this call to service
        // install, update and uninstall hooks before any window is created.
        VelopackApp.Build().Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
