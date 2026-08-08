using BPTracker.Domain.Readings;
using BPTracker.Presentation.Trends;

namespace BPTracker.Presentation.Charts;

/// <summary>
/// The pressure range the vertical axis covers.
/// </summary>
/// <param name="Lowest">Pressure at the bottom edge, in mmHg.</param>
/// <param name="Highest">Pressure at the top edge, in mmHg.</param>
public readonly record struct ChartValueBounds(int Lowest, int Highest)
{
    /// <summary>Axis values are snapped to this multiple so the grid lines land on round numbers.</summary>
    public const int Step = 10;

    /// <summary>Breathing room in mmHg kept above and below the extremes before snapping.</summary>
    private const int Padding = 5;

    /// <summary>
    /// Bounds that cover every sample and both healthy bands, so the reader can always see where
    /// the healthy range sits even when every measurement is far away from it.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="samples"/> is <see langword="null"/>.</exception>
    public static ChartValueBounds For(IReadOnlyList<ChartSample> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);

        var lowest = HealthyRange.Diastolic.Lowest;
        var highest = HealthyRange.Systolic.TooHigh;

        foreach (var sample in samples)
        {
            lowest = Math.Min(lowest, sample.Diastolic);
            highest = Math.Max(highest, sample.Systolic);
        }

        return Create(lowest, highest);
    }

    /// <summary>Bounds that cover every daily average and both healthy bands.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="points"/> is <see langword="null"/>.</exception>
    public static ChartValueBounds ForTrend(IReadOnlyList<TrendChartSample> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        var lowest = (double)HealthyRange.Diastolic.Lowest;
        var highest = (double)HealthyRange.Systolic.TooHigh;

        foreach (var point in points)
        {
            lowest = Math.Min(lowest, point.Diastolic);
            highest = Math.Max(highest, point.Systolic);
        }

        return Create(lowest, highest);
    }

    private static ChartValueBounds Create(double lowest, double highest) =>
        new(FloorTo(lowest - Padding), CeilingTo(highest + Padding));

    private static int FloorTo(double value) => (int)Math.Floor(value / Step) * Step;

    private static int CeilingTo(double value) => (int)Math.Ceiling(value / Step) * Step;
}
