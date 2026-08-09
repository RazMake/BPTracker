using System.Collections.ObjectModel;
using System.Globalization;
using BPTracker.Application.Trends;
using BPTracker.Domain.Readings;
using BPTracker.Domain.Trends;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BPTracker.Presentation.Trends;

/// <summary>
/// Drives the desktop trend chart: which page of which window is loaded, where it is scrolled to,
/// the series and the headline numbers.
/// </summary>
public sealed partial class TrendViewModel : ObservableObject
{
    private const double NotchesPerScreen = 10d;

    private readonly GetTrendUseCase _getTrend;
    private TrendViewport _viewport;
    private TrendWindow _window;
    private DateTimeOffset? _earliestOnRecord;

    [ObservableProperty]
    public partial TrendPeriod Period { get; set; } = TrendPeriod.Month;

    [ObservableProperty]
    public partial TrendSummary Summary { get; set; } = TrendSummary.Empty;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>How far the chart is scrolled from the oldest day of the loaded window, in days.</summary>
    [ObservableProperty]
    public partial double ScrollOffsetDays { get; set; }

    /// <summary>Creates the view model.</summary>
    public TrendViewModel(GetTrendUseCase getTrend)
    {
        _getTrend = getTrend ?? throw new ArgumentNullException(nameof(getTrend));
        _viewport = TrendViewport.For(_window, 0d);
    }

    /// <summary>Daily averages, oldest first.</summary>
    public ObservableCollection<TrendPoint> Daily { get; } = [];

    /// <summary>Exact readings in the loaded window, oldest first.</summary>
    public ObservableCollection<BloodPressureReading> Readings { get; } = [];

    /// <summary>Moving average of <see cref="Daily"/>.</summary>
    public ObservableCollection<TrendPoint> Smoothed { get; } = [];

    /// <summary>Exact, index-aligned values rendered by the desktop chart.</summary>
    public ObservableCollection<TrendChartSample> ChartSamples { get; } = [];

    /// <summary>The windows a user can pick between.</summary>
    public static IReadOnlyList<TrendPeriod> AvailablePeriods { get; } =
        [TrendPeriod.Week, TrendPeriod.Month, TrendPeriod.Quarter, TrendPeriod.Year];

    /// <summary>Which page is loaded. Zero is the most recent one.</summary>
    public int PageIndex { get; private set; }

    /// <summary>The slice of the loaded window the chart draws.</summary>
    public TrendViewport Viewport
    {
        get => _viewport;
        private set => SetProperty(ref _viewport, value);
    }

    /// <summary>The loaded window, spelled out for the page indicator.</summary>
    public string WindowLabel => string.Create(
        CultureInfo.CurrentCulture,
        $"{_window.From:d MMM yyyy} - {_window.To:d MMM yyyy}");

    /// <summary>Whether anything on record was measured before the loaded window.</summary>
    public bool CanPageOlder => _earliestOnRecord is not null && _earliestOnRecord < _window.From;

    /// <summary>Whether a more recent page exists.</summary>
    public bool CanPageNewer => PageIndex > 0;

    /// <summary>Whether the current window contains anything to draw.</summary>
    public bool HasData => Readings.Count > 0;

    /// <summary>Reloads the series for the loaded page.</summary>
    [RelayCommand]
    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        try
        {
            var result = await _getTrend
                .ExecuteAsync(new TrendRequest { Period = Period, PageIndex = PageIndex }, cancellationToken)
                .ConfigureAwait(true);

            Replace(Readings, result.Readings);
            Replace(Daily, result.Daily);
            Replace(Smoothed, result.Smoothed);
            Replace(ChartSamples, TrendChartSampleBuilder.Build(result.Readings, result.Smoothed));
            Summary = result.Summary;
            _earliestOnRecord = result.EarliestMeasuredAt;
            MoveTo(result.Window);
            NotifyPagingChanged();
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Loads the window before the one on screen.</summary>
    [RelayCommand(CanExecute = nameof(CanPageOlder))]
    public Task PageOlderAsync(CancellationToken cancellationToken) =>
        GoToPageAsync(PageIndex + 1, cancellationToken);

    /// <summary>Loads the window after the one on screen.</summary>
    [RelayCommand(CanExecute = nameof(CanPageNewer))]
    public Task PageNewerAsync(CancellationToken cancellationToken) =>
        GoToPageAsync(PageIndex - 1, cancellationToken);

    /// <summary>Scrolls the chart by a number of days, stopping at either end of the window.</summary>
    public void ScrollBy(double days) =>
        ScrollOffsetDays = Math.Clamp(ScrollOffsetDays + days, 0d, Viewport.MaxOffsetDays);

    /// <summary>Scrolls by mouse wheel notches; one notch moves a tenth of what is on screen.</summary>
    public void ScrollByNotches(int notches) =>
        ScrollBy(notches * Viewport.VisibleDays / NotchesPerScreen);

    /// <summary>Puts the newest day of the loaded window against the right edge.</summary>
    public void ScrollToNewest() => ScrollOffsetDays = Viewport.MaxOffsetDays;

    partial void OnPeriodChanged(TrendPeriod value)
    {
        PageIndex = 0;
        RefreshCommand.Execute(null);
    }

    partial void OnScrollOffsetDaysChanged(double value) => Viewport = TrendViewport.For(_window, value);

    private async Task GoToPageAsync(int pageIndex, CancellationToken cancellationToken)
    {
        PageIndex = Math.Max(pageIndex, 0);
        await RefreshAsync(cancellationToken).ConfigureAwait(true);
    }

    // The viewport has to be rebuilt against the new window before the offset can be clamped to it.
    private void MoveTo(TrendWindow window)
    {
        _window = window;
        Viewport = TrendViewport.For(window, ScrollOffsetDays);
        ScrollToNewest();
    }

    private void NotifyPagingChanged()
    {
        OnPropertyChanged(nameof(HasData));
        OnPropertyChanged(nameof(PageIndex));
        OnPropertyChanged(nameof(WindowLabel));
        OnPropertyChanged(nameof(CanPageOlder));
        OnPropertyChanged(nameof(CanPageNewer));
        PageOlderCommand.NotifyCanExecuteChanged();
        PageNewerCommand.NotifyCanExecuteChanged();
    }

    private static void Replace<T>(ObservableCollection<T> target, IReadOnlyList<T> source)
    {
        target.Clear();
        foreach (var point in source)
        {
            target.Add(point);
        }
    }
}
