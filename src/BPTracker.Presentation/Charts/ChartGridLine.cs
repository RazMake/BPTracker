namespace BPTracker.Presentation.Charts;

/// <summary>
/// A horizontal reference line on the value axis.
/// </summary>
/// <param name="Y">Where to draw it, in plot pixels.</param>
/// <param name="Value">The pressure it marks, in mmHg.</param>
/// <param name="IsHealthyEdge">Whether it is an edge of a healthy band rather than a plain gridline.</param>
public readonly record struct ChartGridLine(double Y, int Value, bool IsHealthyEdge);
