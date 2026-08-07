namespace BPTracker.Presentation.Charts;

/// <summary>
/// A point in the plot area's own pixel space, with the origin at its top left corner.
/// </summary>
/// <param name="X">Horizontal offset in device-independent pixels.</param>
/// <param name="Y">Vertical offset in device-independent pixels, increasing downwards.</param>
public readonly record struct ChartPoint(double X, double Y);
