namespace BPTracker.Presentation.Charts;

/// <summary>
/// A marker drawn at one measurement.
/// </summary>
/// <param name="At">Where to draw it.</param>
/// <param name="IsTagged">Whether the reading carries a tag, and so deserves a larger marker.</param>
/// <param name="IsCritical">Whether the value is at or above its crisis threshold.</param>
public readonly record struct ChartDot(ChartPoint At, bool IsTagged, bool IsCritical);
