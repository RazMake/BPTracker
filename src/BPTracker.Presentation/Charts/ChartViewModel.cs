using BPTracker.Application.Readings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BPTracker.Presentation.Charts;

/// <summary>
/// Drives the phone's history chart: what is loaded, how far it is scrolled, and where the
/// user's finger is. The view only draws the <see cref="ChartFrame"/> this produces.
/// </summary>
public sealed partial class ChartViewModel : ObservableObject
{
    /// <summary>How far back the chart reaches. Long enough to mean "everything recorded".</summary>
    public const int WindowDays = 3650;

    private readonly GetReadingHistoryUseCase _getHistory;
    private readonly List<ChartSample> _samples = [];
    private double _scrollAnchor;
    private bool _pinnedToNewest = true;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>Creates the view model.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="getHistory"/> is <see langword="null"/>.</exception>
    public ChartViewModel(GetReadingHistoryUseCase getHistory) =>
        _getHistory = getHistory ?? throw new ArgumentNullException(nameof(getHistory));

    /// <summary>Raised whenever the drawn output would change, so the view can invalidate itself.</summary>
    public event EventHandler? FrameChanged;

    /// <summary>Every loaded measurement, oldest first.</summary>
    public IReadOnlyList<ChartSample> Samples => _samples;

    /// <summary>Whether there is anything to draw.</summary>
    public bool HasData => _samples.Count > 0;

    /// <summary>Width of the plot area, in pixels.</summary>
    public double PlotWidth { get; private set; }

    /// <summary>Height of the plot area, in pixels.</summary>
    public double PlotHeight { get; private set; }

    /// <summary>Horizontal zoom.</summary>
    public double PixelsPerHour { get; } = ChartRequest.DefaultPixelsPerHour;

    /// <summary>How far the content is scrolled left, in pixels.</summary>
    public double Offset { get; private set; }

    /// <summary>Where the user is holding a finger, or <see langword="null"/> when nothing is held.</summary>
    public double? CursorX { get; private set; }

    /// <summary>Width of the whole series at the current zoom.</summary>
    public double ContentWidth => ChartScroll.ContentWidth(_samples, PixelsPerHour, PlotWidth);

    /// <summary>Whether the series is wider than the screen.</summary>
    public bool CanScroll => ContentWidth > PlotWidth;

    /// <summary>Loads every recorded measurement and shows the most recent ones.</summary>
    [RelayCommand]
    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        try
        {
            var readings = await _getHistory
                .ExecuteAsync(WindowDays, cancellationToken)
                .ConfigureAwait(true);

            _samples.Clear();
            _samples.AddRange(readings
                .OrderBy(reading => reading.MeasuredAt)
                .Select(ChartSample.From));

            CursorX = null;
            _pinnedToNewest = true;
            ScrollToNewest();
            OnPropertyChanged(nameof(HasData));
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Tells the chart how much room it has.</summary>
    public void Resize(double width, double height)
    {
        PlotWidth = Math.Max(width, 0);
        PlotHeight = Math.Max(height, 0);
        Offset = _pinnedToNewest ? Newest() : ChartScroll.Clamp(Offset, ContentWidth, PlotWidth);
        Notify();
    }

    /// <summary>Decides what a touch at the given height means.</summary>
    public ChartGesture GestureFor(double y) => ChartTouch.GestureFor(y, PlotHeight);

    /// <summary>Remembers where a scroll gesture started.</summary>
    public void BeginScroll() => _scrollAnchor = Offset;

    /// <summary>Scrolls by the total movement since <see cref="BeginScroll"/>. Dragging left moves forward in time.</summary>
    public void Scroll(double totalDeltaX)
    {
        var newest = Newest();
        Offset = ChartScroll.Clamp(_scrollAnchor - totalDeltaX, ContentWidth, PlotWidth);
        _pinnedToNewest = Offset >= newest;
        Notify();
    }

    /// <summary>Jumps to the most recent measurement.</summary>
    public void ScrollToNewest()
    {
        Offset = Newest();
        _pinnedToNewest = true;
        Notify();
    }

    /// <summary>Shows the read-out for the measurement nearest the given position.</summary>
    public void Inspect(double x)
    {
        CursorX = x;
        Notify();
    }

    /// <summary>Hides the read-out.</summary>
    public void ClearCursor()
    {
        CursorX = null;
        Notify();
    }

    /// <summary>Builds everything the view needs to paint one frame.</summary>
    public ChartFrame BuildFrame() => ChartFrameBuilder.Build(new ChartRequest
    {
        Samples = _samples,
        PlotWidth = PlotWidth,
        PlotHeight = PlotHeight,
        PixelsPerHour = PixelsPerHour,
        Offset = Offset,
        CursorX = CursorX,
    });

    private double Newest() => Math.Max(ContentWidth - PlotWidth, 0);

    private void Notify() => FrameChanged?.Invoke(this, EventArgs.Empty);
}
