namespace BPTracker.Domain.Readings;

/// <summary>
/// The pressure at or above which a reading is a hypertensive crisis.
/// </summary>
/// <remarks>
/// Held here rather than inside <see cref="BloodPressureClassifier"/> so a chart can shade the
/// same boundary the classifier judges by.
/// </remarks>
public static class CrisisThreshold
{
    /// <summary>Systolic at or above this value is a hypertensive crisis.</summary>
    public const int Systolic = 181;

    /// <summary>Diastolic at or above this value is a hypertensive crisis.</summary>
    public const int Diastolic = 121;
}
