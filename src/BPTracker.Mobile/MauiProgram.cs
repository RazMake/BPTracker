using BPTracker.Application.Abstractions;
using BPTracker.Application.Readings;
using BPTracker.Application.Trends;
using BPTracker.Infrastructure.Storage;
using BPTracker.Infrastructure.Time;
using BPTracker.Presentation.Charts;
using BPTracker.Presentation.Readings;
using BPTracker.Presentation.Storage;
using Microsoft.Extensions.Logging;

namespace BPTracker.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<IStorageLocation>(_ => new StorageLocation(
            MobileStorage.SettingsFolder(),
            MobileStorage.DefaultDataFolder()));
        builder.Services.AddSingleton<IReadingRepository, JournalReadingRepository>();
        builder.Services.AddSingleton<IClock, SystemClock>();

        builder.Services.AddTransient<AddReadingUseCase>();
        builder.Services.AddTransient<GetReadingHistoryUseCase>();
        builder.Services.AddTransient<RetractReadingUseCase>();
        builder.Services.AddTransient<GetTrendUseCase>();

        builder.Services.AddTransient<ReadingEntryViewModel>();
        builder.Services.AddTransient<HistoryViewModel>();
        builder.Services.AddTransient<ChartViewModel>();
        builder.Services.AddTransient<StorageLocationViewModel>();
        builder.Services.AddTransient<EntryPage>();
        builder.Services.AddTransient<ChartPage>();
        builder.Services.AddTransient<SettingsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
