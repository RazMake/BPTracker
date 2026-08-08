namespace BPTracker.Presentation.Charts;

/// <summary>
/// A request to paint one chart frame.
/// </summary>
public sealed record ChartRequest
{
    /// <summary>Default horizontal zoom: 30 pixels a day, so a phone shows roughly ten days.</summary>
    public const double DefaultPixelsPerHour = 1.25d;

    /// <summary>Samples in time order, oldest first.</summary>
    public required IReadOnlyList<ChartSample> Samples { get; init; }

    /// <summary>Width of the plot area in pixels.</summary>
    public required double PlotWidth { get; init; }

    /// <summary>Height of the plot area in pixels.</summary>
    public required double PlotHeight { get; init; }

    /// <summary>Horizontal zoom.</summary>
    public double PixelsPerHour { get; init; } = DefaultPixelsPerHour;

    /// <summary>How far the content is scrolled left, in pixels.</summary>
    public double Offset { get; init; }

    /// <summary>Where the user is holding a finger, or <see langword="null"/> when nothing is held.</summary>
    public double? CursorX { get; init; }
}
