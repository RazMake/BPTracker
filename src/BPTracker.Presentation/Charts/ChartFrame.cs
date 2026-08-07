namespace BPTracker.Presentation.Charts;

/// <summary>
/// Everything needed to paint the chart once. The view draws this and nothing else, which keeps
/// the chart's maths in a project the coverage gate measures.
/// </summary>
public sealed record ChartFrame
{
    /// <summary>A frame with nothing to draw.</summary>
    public static ChartFrame Empty { get; } = new();

    /// <summary>Shaded corridors behind the lines, lowest first.</summary>
    public IReadOnlyList<ChartBandArea> Bands { get; init; } = [];

    /// <summary>The systolic line.</summary>
    public ChartLine Systolic { get; init; } = ChartLine.Empty;

    /// <summary>The diastolic line.</summary>
    public ChartLine Diastolic { get; init; } = ChartLine.Empty;

    /// <summary>Horizontal reference lines, bottom value first.</summary>
    public IReadOnlyList<ChartGridLine> GridLines { get; init; } = [];

    /// <summary>Ticks on the time axis, oldest first.</summary>
    public IReadOnlyList<ChartTimeLabel> TimeLabels { get; init; } = [];

    /// <summary>The read-out under the user's finger, or <see langword="null"/> when nothing is held.</summary>
    public ChartCursor? Cursor { get; init; }

    /// <summary>Whether there is any measurement to show.</summary>
    public bool HasData => Systolic.Dots.Count > 0;
}
