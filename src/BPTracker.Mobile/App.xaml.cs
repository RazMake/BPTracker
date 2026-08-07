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
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new NavigationPage(_services.GetRequiredService<EntryPage>()));
}
