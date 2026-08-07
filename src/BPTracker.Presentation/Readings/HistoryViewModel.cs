using System.Collections.ObjectModel;
using BPTracker.Application.Readings;
using BPTracker.Domain.Readings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BPTracker.Presentation.Readings;

/// <summary>
/// Drives the history list and the retract action.
/// </summary>
public sealed partial class HistoryViewModel : ObservableObject
{
    /// <summary>How far back the list reaches by default.</summary>
    public const int DefaultWindowDays = 90;

    private readonly GetReadingHistoryUseCase _getHistory;
    private readonly RetractReadingUseCase _retractReading;

    [ObservableProperty]
    public partial int WindowDays { get; set; } = DefaultWindowDays;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>Creates the view model.</summary>
    public HistoryViewModel(GetReadingHistoryUseCase getHistory, RetractReadingUseCase retractReading)
    {
        _getHistory = getHistory ?? throw new ArgumentNullException(nameof(getHistory));
        _retractReading = retractReading ?? throw new ArgumentNullException(nameof(retractReading));
    }

    /// <summary>Readings in the window, newest first.</summary>
    public ObservableCollection<BloodPressureReading> Readings { get; } = [];

    /// <summary>The most recent reading, used to seed the entry screen.</summary>
    public BloodPressureReading? Latest => Readings.Count > 0 ? Readings[0] : null;

    /// <summary>Raised whenever the list changes, so dependent views can refresh.</summary>
    public event EventHandler? Changed;

    /// <summary>Reloads the list.</summary>
    [RelayCommand]
    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        try
        {
            var readings = await _getHistory
                .ExecuteAsync(WindowDays, cancellationToken)
                .ConfigureAwait(true);

            Readings.Clear();
            foreach (var reading in readings)
            {
                Readings.Add(reading);
            }

            OnPropertyChanged(nameof(Latest));
            Changed?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Retracts a reading and reloads the list.</summary>
    [RelayCommand]
    public async Task RetractAsync(BloodPressureReading? reading, CancellationToken cancellationToken)
    {
        if (reading is null)
        {
            return;
        }

        await _retractReading.ExecuteAsync(reading.Id, cancellationToken).ConfigureAwait(true);
        await RefreshAsync(cancellationToken).ConfigureAwait(true);
    }
}
