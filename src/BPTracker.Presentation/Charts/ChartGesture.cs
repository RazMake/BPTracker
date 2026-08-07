namespace BPTracker.Presentation.Charts;

/// <summary>
/// What a touch on the chart means, decided by where on the chart it landed.
/// </summary>
public enum ChartGesture
{
    /// <summary>Drag the chart sideways through time.</summary>
    Scroll = 0,

    /// <summary>Hold a vertical line against the nearest measurement and read its numbers.</summary>
    Inspect = 1,
}
