namespace BPTracker.Presentation.Charts;

/// <summary>
/// A shaded corridor across the plot area, in plot pixels.
/// </summary>
/// <param name="Top">Top edge, in plot pixels.</param>
/// <param name="Bottom">Bottom edge, in plot pixels.</param>
/// <param name="Label">Which series the corridor is normal for.</param>
public readonly record struct ChartBandArea(double Top, double Bottom, string Label)
{
    /// <summary>Height of the corridor, in plot pixels.</summary>
    public double Height => Bottom - Top;
}
