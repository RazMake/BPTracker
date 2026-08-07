using BPTracker.Domain.Readings;

namespace BPTracker.Presentation.Charts;

/// <summary>
/// The pressure range the vertical axis covers.
/// </summary>
/// <param name="Lowest">Pressure at the bottom edge, in mmHg.</param>
/// <param name="Highest">Pressure at the top edge, in mmHg.</param>
public readonly record struct ChartValueBounds(int Lowest, int Highest)
{
    /// <summary>Axis values are snapped to this multiple so the grid lines land on round numbers.</summary>
    public const int Step = 20;

    /// <summary>Breathing room in mmHg kept above and below the extremes before snapping.</summary>
    private const int Padding = 10;

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

        return new ChartValueBounds(FloorTo(lowest - Padding), CeilingTo(highest + Padding));
    }

    private static int FloorTo(int value) => value / Step * Step;

    private static int CeilingTo(int value) => (value + Step - 1) / Step * Step;
}
