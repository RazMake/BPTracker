using Microsoft.Extensions.DependencyInjection;

namespace BPTracker.Mobile;

// Our own BPTracker.Application namespace shadows Microsoft.Maui.Controls.Application,
// so the base type has to be fully qualified.
public partial class App : Microsoft.Maui.Controls.Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        _services = services;
        InitializeComponent();

        // Dark only, and set here rather than left to the system so Android's own dialogs,
        // pickers and keyboards follow the app instead of the phone's day/night setting.
        UserAppTheme = AppTheme.Dark;
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new NavigationPage(_services.GetRequiredService<EntryPage>()));
}
