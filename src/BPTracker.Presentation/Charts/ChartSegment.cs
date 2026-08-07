namespace BPTracker.Presentation.Charts;

/// <summary>
/// A straight run of a plotted line, split where it crosses its crisis threshold so that the
/// crisis part can be drawn in a different colour from the rest.
/// </summary>
/// <param name="From">Where the run starts.</param>
/// <param name="To">Where the run ends.</param>
/// <param name="IsCritical">Whether the whole run is at or above the crisis threshold.</param>
public readonly record struct ChartSegment(ChartPoint From, ChartPoint To, bool IsCritical);
