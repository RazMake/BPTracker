namespace BPTracker.Presentation.Charts;

/// <summary>
/// One plotted series: the runs that make up its line, and a marker per measurement.
/// </summary>
/// <param name="Segments">Runs in time order. Empty when there are fewer than two measurements.</param>
/// <param name="Dots">One marker per measurement, in time order.</param>
public sealed record ChartLine(IReadOnlyList<ChartSegment> Segments, IReadOnlyList<ChartDot> Dots)
{
    /// <summary>A line with nothing to draw.</summary>
    public static ChartLine Empty { get; } = new([], []);
}
