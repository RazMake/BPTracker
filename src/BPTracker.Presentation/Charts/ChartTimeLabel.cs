namespace BPTracker.Presentation.Charts;

/// <summary>
/// A tick on the time axis.
/// </summary>
/// <param name="X">Where to draw it, in plot pixels.</param>
/// <param name="Text">The date to print under it.</param>
public readonly record struct ChartTimeLabel(double X, string Text);
