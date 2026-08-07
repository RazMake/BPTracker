namespace BPTracker.Domain.Readings;

/// <summary>
/// Where a single pressure value sits relative to its healthy band.
/// </summary>
/// <remarks>
/// Deliberately coarser than <see cref="BloodPressureCategory"/>. A category needs both numbers;
/// a zone describes one number, which is what a single line on a chart can show.
/// </remarks>
public enum PressureZone
{
    /// <summary>Below the healthy band.</summary>
    Low = 0,

    /// <summary>Inside the healthy band.</summary>
    Healthy = 1,

    /// <summary>Above the healthy band.</summary>
    High = 2,
}
