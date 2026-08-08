using BPTracker.Application.Abstractions;
using BPTracker.Application.Readings;
using BPTracker.Application.Trends;
using BPTracker.Infrastructure.Storage;
using BPTracker.Infrastructure.Time;
using BPTracker.Presentation;
using BPTracker.Presentation.Export;
using BPTracker.Presentation.Readings;
using BPTracker.Presentation.Storage;
using BPTracker.Presentation.Trends;
using Microsoft.Extensions.DependencyInjection;

namespace BPTracker.Desktop;

/// <summary>
/// Composition root. The only place in the desktop app that knows about concrete implementations.
/// </summary>
public static class DesktopServices
{
    public static ServiceProvider Build()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IStorageLocation>(_ => new StorageLocation(
            SettingsFolder(),
            DefaultDataFolder()));
        services.AddSingleton<IReadingRepository, JournalReadingRepository>();
        services.AddSingleton<IClock, SystemClock>();

        services.AddTransient<AddReadingUseCase>();
        services.AddTransient<GetReadingHistoryUseCase>();
        services.AddTransient<RetractReadingUseCase>();
        services.AddTransient<GetTrendUseCase>();

        services.AddTransient<ReadingEntryViewModel>();
        services.AddTransient<HistoryViewModel>();
        services.AddTransient<TrendViewModel>();
        services.AddTransient<StorageLocationViewModel>();
        services.AddSingleton<DesktopExportRenderer>();
        services.AddSingleton<IExportRenderer>(provider => provider.GetRequiredService<DesktopExportRenderer>());
        services.AddSingleton<ExportViewModel>();
        services.AddSingleton<DashboardViewModel>();

        services.AddTransient<MainWindow>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Per-device settings. Kept beside the app, never in the synced folder, and under
    /// LocalApplicationData so it survives Velopack updates replacing the install directory.
    /// </summary>
    public static string SettingsFolder() => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BPTracker");

    /// <summary>
    /// Default location of the reading journals. Documents so the user can find it, point their
    /// sync tool at it, and open the files in a text editor.
    /// </summary>
    public static string DefaultDataFolder() => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "BPTracker");
}
