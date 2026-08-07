namespace BPTracker.Domain.Readings;

/// <summary>
/// Classifies a reading into a <see cref="BloodPressureCategory"/> using the ACC/AHA 2017 bands.
/// </summary>
public static class BloodPressureClassifier
{
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

        if (sbp >= CrisisThreshold.Systolic || dbp >= CrisisThreshold.Diastolic)
        {
            return BloodPressureCategory.HypertensiveCrisis;
        }

        if (sbp < HealthyRange.Systolic.Lowest || dbp < HealthyRange.Diastolic.Lowest)
        {
            return BloodPressureCategory.Hypotension;
        }

        if (sbp >= 140 || dbp >= 90)
        {
            return BloodPressureCategory.HypertensionStage2;
        }

        if (sbp >= 130 || dbp >= HealthyRange.Diastolic.TooHigh)
        {
            return BloodPressureCategory.HypertensionStage1;
        }

        return sbp >= HealthyRange.Systolic.TooHigh
            ? BloodPressureCategory.Elevated
            : BloodPressureCategory.Normal;
    }
}
