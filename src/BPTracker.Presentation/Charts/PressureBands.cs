using BPTracker.Domain.Readings;

namespace BPTracker.Presentation.Charts;

/// <summary>
/// The corridors a chart shades behind its lines: the values that are normal for each series.
/// </summary>
/// <remarks>
/// Only the normal ranges are shaded. Shading the bad ranges too was tried and covered most of the
/// chart, which left the shading meaning nothing. A crisis is called out on the line itself
/// instead, in red.
/// </remarks>
public static class PressureBands
{
    /// <summary>Label for the corridor in which systolic pressure is normal.</summary>
    public const string SystolicLabel = "SYSTOLIC NORMAL";

    /// <summary>Label for the corridor in which diastolic pressure is normal.</summary>
    public const string DiastolicLabel = "DIASTOLIC NORMAL";

    /// <summary>
    /// The normal corridors that fall inside the given axis, lowest first, clipped to it.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The axis is inverted or empty.</exception>
    public static IReadOnlyList<PressureBand> For(int lowest, int highest)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(highest, lowest);

        var bands = new List<PressureBand>(2);

        void Add(HealthyRange range, string label)
        {
            var from = Math.Max(range.Lowest, lowest);
            var to = Math.Min(range.TooHigh, highest);

            if (to > from)
            {
                bands.Add(new PressureBand(from, to, label));
            }
        }

        Add(HealthyRange.Diastolic, DiastolicLabel);
        Add(HealthyRange.Systolic, SystolicLabel);

        return bands;
    }
}
