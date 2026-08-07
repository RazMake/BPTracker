using BPTracker.Application.Readings;
using BPTracker.Application.Trends;
using BPTracker.Presentation.Readings;
using BPTracker.Presentation.Storage;
using BPTracker.Presentation.Trends;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BPTracker.Presentation;

/// <summary>
/// Composes the entry, history, trend and storage view models and keeps them in step.
/// </summary>
/// <remarks>
/// Saving a reading has to refresh both the history list and the chart, and changing the data
/// folder has to reload everything. Owning that coordination here keeps it testable instead of
/// stranded in window code-behind.
/// </remarks>
public sealed partial class DashboardViewModel : ObservableObject, IDisposable
{
    /// <summary>Creates the dashboard.</summary>
    public DashboardViewModel(
        ReadingEntryViewModel entry,
        HistoryViewModel history,
        TrendViewModel trend,
        StorageLocationViewModel storage)
    {
        Entry = entry ?? throw new ArgumentNullException(nameof(entry));
        History = history ?? throw new ArgumentNullException(nameof(history));
        Trend = trend ?? throw new ArgumentNullException(nameof(trend));
        Storage = storage ?? throw new ArgumentNullException(nameof(storage));

        Entry.ReadingSaved += OnReadingSaved;
        Storage.Changed += OnStorageChanged;
    }

    /// <summary>The fast entry panel.</summary>
    public ReadingEntryViewModel Entry { get; }

    /// <summary>The history list.</summary>
    public HistoryViewModel History { get; }

    /// <summary>The trend chart.</summary>
    public TrendViewModel Trend { get; }

    /// <summary>Where readings are stored, shown to the user and changeable.</summary>
    public StorageLocationViewModel Storage { get; }

    /// <summary>Loads the initial data and seeds the entry fields from the latest reading.</summary>
    [RelayCommand]
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await RefreshAsync(cancellationToken).ConfigureAwait(true);
        Entry.SeedFrom(History.Latest);
    }

    /// <summary>Reloads the history list and the chart.</summary>
    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        await History.RefreshAsync(cancellationToken).ConfigureAwait(true);
        await Trend.RefreshAsync(cancellationToken).ConfigureAwait(true);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Entry.ReadingSaved -= OnReadingSaved;
        Storage.Changed -= OnStorageChanged;
        Storage.Detach();
    }

    /// <summary>Raised when a background refresh fails, so the shell can surface it.</summary>
    public event EventHandler<Exception>? RefreshFailed;

    private async void OnReadingSaved(object? sender, Domain.Readings.BloodPressureReading reading) =>
        await SafeRefreshAsync().ConfigureAwait(true);

    private async void OnStorageChanged(object? sender, EventArgs e) =>
        await SafeRefreshAsync().ConfigureAwait(true);

    private async Task SafeRefreshAsync()
    {
        try
        {
            await RefreshAsync(CancellationToken.None).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            // An event handler must never let an exception escape onto the UI thread.
            RefreshFailed?.Invoke(this, exception);
        }
    }
}
