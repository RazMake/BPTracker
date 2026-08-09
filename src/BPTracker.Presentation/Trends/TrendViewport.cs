using BPTracker.Application.Trends;

namespace BPTracker.Presentation.Trends;

/// <summary>
/// The slice of a loaded window the trend chart draws, and how far it can still be scrolled.
/// </summary>
/// <remarks>
/// The chart keeps a fixed number of days on screen so the distance between two readings never
/// depends on how much history is loaded. Anything wider than that is reached by scrolling
/// instead of by compressing the whole window into the plot.
/// </remarks>
public readonly record struct TrendViewport
{
    /// <summary>Days on screen at once, which is what a month's window used to fill.</summary>
    public const double DefaultVisibleDays = 30d;

    private const double LabelsPerScreen = 10d;

    private TrendViewport(DateTimeOffset from, double visibleDays, double offsetDays, double maxOffsetDays)
    {
        From = from;
        VisibleDays = visibleDays;
        OffsetDays = offsetDays;
        MaxOffsetDays = maxOffsetDays;
    }

    /// <summary>The oldest instant on screen.</summary>
    public DateTimeOffset From { get; }

    /// <summary>The newest instant on screen.</summary>
    public DateTimeOffset To => From.AddDays(VisibleDays);

    /// <summary>How many days are on screen.</summary>
    public double VisibleDays { get; }

    /// <summary>How far the viewport sits from the oldest day of the window, in days.</summary>
    public double OffsetDays { get; }

    /// <summary>The largest offset that still fills the viewport.</summary>
    public double MaxOffsetDays { get; }

    /// <summary>Whether the loaded window is wider than the viewport.</summary>
    public bool CanScroll => MaxOffsetDays > 0d;

    /// <summary>Days between date labels, kept at roughly ten labels across.</summary>
    public double LabelStepDays => Math.Max(1d, Math.Ceiling(VisibleDays / LabelsPerScreen));

    /// <summary>
    /// Places the viewport over <paramref name="window"/>, clamping the offset to the window.
    /// </summary>
    /// <param name="window">The loaded range.</param>
    /// <param name="offsetDays">Days between the oldest day of the window and the left edge.</param>
    /// <param name="visibleDays">How many days to keep on screen.</param>
    public static TrendViewport For(
        TrendWindow window,
        double offsetDays,
        double visibleDays = DefaultVisibleDays)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(visibleDays);

        var windowDays = Math.Max(window.Length.TotalDays, 0d);
        var visible = windowDays > 0d ? Math.Min(visibleDays, windowDays) : visibleDays;
        var maxOffset = Math.Max(windowDays - visible, 0d);
        var offset = Math.Clamp(offsetDays, 0d, maxOffset);

        return new TrendViewport(window.From.AddDays(offset), visible, offset, maxOffset);
    }
}
