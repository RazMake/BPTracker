namespace BPTracker.Presentation.Charts;

/// <summary>
/// A corridor on the value axis in which one of the two series is normal.
/// </summary>
/// <param name="Lowest">Bottom of the corridor in mmHg, inclusive.</param>
/// <param name="Highest">Top of the corridor in mmHg, exclusive.</param>
/// <param name="Label">Which series the corridor is normal for.</param>
public readonly record struct PressureBand(int Lowest, int Highest, string Label);
