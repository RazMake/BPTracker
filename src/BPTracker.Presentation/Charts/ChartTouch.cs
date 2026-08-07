namespace BPTracker.Presentation.Charts;

/// <summary>
/// Splits the chart into a band that scrolls and a band that inspects, so a single-finger
/// gesture can do both without a mode switch.
/// </summary>
public static class ChartTouch
{
    /// <summary>Fraction of the plot height, measured from the top, that scrolls rather than inspects.</summary>
    public const double ScrollBandFraction = 0.55d;

    /// <summary>Decides what a touch at the given height means.</summary>
    public static ChartGesture GestureFor(double y, double plotHeight) =>
        plotHeight > 0 && y > plotHeight * ScrollBandFraction
            ? ChartGesture.Inspect
            : ChartGesture.Scroll;
}
