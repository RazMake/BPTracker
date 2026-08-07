namespace BPTracker.Domain.Readings;

/// <summary>
/// Classifies a reading into a <see cref="BloodPressureCategory"/> using the ACC/AHA 2017 bands.
/// </summary>
public static class BloodPressureClassifier
{
    /// <summary>Systolic at or above this value is a hypertensive crisis.</summary>
    private const int CrisisSystolic = 181;

    /// <summary>Diastolic at or above this value is a hypertensive crisis.</summary>
    private const int CrisisDiastolic = 121;

    /// <summary>
    /// Determines the category for the supplied pressures.
    /// </summary>
    /// <remarks>
    /// Evaluation order is significant: the bands overlap, and the guideline assigns a reading to
    /// the most severe band it qualifies for.
    /// </remarks>
    public static BloodPressureCategory Classify(SystolicPressure systolic, DiastolicPressure diastolic)
    {
        var sbp = systolic.MmHg;
        var dbp = diastolic.MmHg;

        if (sbp >= CrisisSystolic || dbp >= CrisisDiastolic)
        {
            return BloodPressureCategory.HypertensiveCrisis;
        }

        if (sbp < 90 || dbp < 60)
        {
            return BloodPressureCategory.Hypotension;
        }

        if (sbp >= 140 || dbp >= 90)
        {
            return BloodPressureCategory.HypertensionStage2;
        }

        if (sbp >= 130 || dbp >= 80)
        {
            return BloodPressureCategory.HypertensionStage1;
        }

        return sbp >= 120
            ? BloodPressureCategory.Elevated
            : BloodPressureCategory.Normal;
    }
}
