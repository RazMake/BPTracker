namespace BPTracker.Presentation.Charts;

/// <summary>
/// Maps measurement time and pressure onto the plot area's pixel space.
/// </summary>
/// <remarks>
/// Time is mapped proportionally: two measurements an hour apart are always
/// <see cref="PixelsPerHour"/> apart, so a gap in the record shows as a gap on the chart.
/// </remarks>
public sealed class ChartScale
{
    /// <summary>Creates a scale.</summary>
    /// <param name="origin">Time that sits at content offset zero, normally the oldest sample.</param>
    /// <param name="pixelsPerHour">Horizontal zoom. Must be above zero.</param>
    /// <param name="offset">How far the content is scrolled left, in pixels.</param>
    /// <param name="plotHeight">Height of the plot area in pixels. Must be above zero.</param>
    /// <param name="lowest">Pressure at the bottom edge, in mmHg.</param>
    /// <param name="highest">Pressure at the top edge, in mmHg. Must exceed <paramref name="lowest"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">A size or zoom is not positive, or the value axis is inverted.</exception>
    public ChartScale(
        DateTimeOffset origin,
        double pixelsPerHour,
        double offset,
        double plotHeight,
        int lowest,
        int highest)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pixelsPerHour);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(plotHeight);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(highest, lowest);

        Origin = origin;
        PixelsPerHour = pixelsPerHour;
        Offset = offset;
        PlotHeight = plotHeight;
        Lowest = lowest;
        Highest = highest;
    }

    /// <summary>Time at content offset zero.</summary>
    public DateTimeOffset Origin { get; }

    /// <summary>Horizontal zoom.</summary>
    public double PixelsPerHour { get; }

    /// <summary>How far the content is scrolled left, in pixels.</summary>
    public double Offset { get; }

    /// <summary>Height of the plot area, in pixels.</summary>
    public double PlotHeight { get; }

    /// <summary>Pressure at the bottom edge, in mmHg.</summary>
    public int Lowest { get; }

    /// <summary>Pressure at the top edge, in mmHg.</summary>
    public int Highest { get; }

    /// <summary>Screen x for an instant.</summary>
    public double X(DateTimeOffset at) => ((at - Origin).TotalHours * PixelsPerHour) - Offset;

    /// <summary>Screen y for a pressure.</summary>
    public double Y(double mmHg) => PlotHeight - ((mmHg - Lowest) / (Highest - Lowest) * PlotHeight);

    /// <summary>Screen position of a measurement.</summary>
    public ChartPoint Point(DateTimeOffset at, double mmHg) => new(X(at), Y(mmHg));

    /// <summary>The instant shown at a screen x.</summary>
    public DateTimeOffset TimeAt(double x) => Origin.AddHours((x + Offset) / PixelsPerHour);
}
