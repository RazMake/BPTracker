namespace BPTracker.Presentation.Charts;

/// <summary>
/// Horizontal scrolling maths for the chart: how wide the content is, and how far it may move.
/// </summary>
public static class ChartScroll
{
    /// <summary>
    /// Width of the whole series at the given zoom, never less than the visible width so a short
    /// history cannot be scrolled away.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="samples"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="pixelsPerHour"/> is not positive.</exception>
    public static double ContentWidth(
        IReadOnlyList<ChartSample> samples,
        double pixelsPerHour,
        double plotWidth)
    {
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pixelsPerHour);

        if (samples.Count == 0)
        {
            return Math.Max(plotWidth, 0);
        }

        var span = samples[^1].MeasuredAt - samples[0].MeasuredAt;
        return Math.Max(span.TotalHours * pixelsPerHour, Math.Max(plotWidth, 0));
    }

    /// <summary>Keeps an offset inside the scrollable range.</summary>
    public static double Clamp(double offset, double contentWidth, double plotWidth) =>
        Math.Clamp(offset, 0, Math.Max(contentWidth - plotWidth, 0));
}
