using System.Collections.ObjectModel;
using BPTracker.Application.Trends;
using BPTracker.Domain.Trends;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BPTracker.Presentation.Trends;

/// <summary>
/// Drives the desktop trend chart: window selection, series and headline numbers.
/// </summary>
public sealed partial class TrendViewModel : ObservableObject
{
    private readonly GetTrendUseCase _getTrend;

    [ObservableProperty]
    public partial TrendPeriod Period { get; set; } = TrendPeriod.Month;

    [ObservableProperty]
    public partial TrendSummary Summary { get; set; } = TrendSummary.Empty;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>Creates the view model.</summary>
    public TrendViewModel(GetTrendUseCase getTrend) =>
        _getTrend = getTrend ?? throw new ArgumentNullException(nameof(getTrend));

    /// <summary>Daily averages, oldest first.</summary>
    public ObservableCollection<TrendPoint> Daily { get; } = [];

    /// <summary>Moving average of <see cref="Daily"/>.</summary>
    public ObservableCollection<TrendPoint> Smoothed { get; } = [];

    /// <summary>The windows a user can pick between.</summary>
    public static IReadOnlyList<TrendPeriod> AvailablePeriods { get; } =
        [TrendPeriod.Week, TrendPeriod.Month, TrendPeriod.Quarter, TrendPeriod.Year, TrendPeriod.All];

    /// <summary>Whether the current window contains anything to draw.</summary>
    public bool HasData => Daily.Count > 0;

    /// <summary>Reloads the series for the selected window.</summary>
    [RelayCommand]
    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        try
        {
            var result = await _getTrend
                .ExecuteAsync(Period, cancellationToken: cancellationToken)
                .ConfigureAwait(true);

            Replace(Daily, result.Daily);
            Replace(Smoothed, result.Smoothed);
            Summary = result.Summary;
            OnPropertyChanged(nameof(HasData));
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnPeriodChanged(TrendPeriod value) => RefreshCommand.Execute(null);

    private static void Replace(ObservableCollection<TrendPoint> target, IReadOnlyList<TrendPoint> source)
    {
        target.Clear();
        foreach (var point in source)
        {
            target.Add(point);
        }
    }
}
