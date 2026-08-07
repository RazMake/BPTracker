namespace BPTracker.Domain.Readings;

/// <summary>
/// Blood pressure category per the ACC/AHA 2017 guideline, plus a hypotension band.
/// </summary>
/// <remarks>
/// This is an informational classification for trend display only. It is not medical advice
/// and must never be presented to the user as a diagnosis.
/// </remarks>
public enum BloodPressureCategory
{
    /// <summary>Systolic below 90 or diastolic below 60.</summary>
    Hypotension,

    /// <summary>Systolic below 120 and diastolic below 80.</summary>
    Normal,

    /// <summary>Systolic 120-129 and diastolic below 80.</summary>
    Elevated,

    /// <summary>Systolic 130-139 or diastolic 80-89.</summary>
    HypertensionStage1,

    /// <summary>Systolic 140 or above, or diastolic 90 or above.</summary>
    HypertensionStage2,

    /// <summary>Systolic above 180 or diastolic above 120. Requires prompt medical attention.</summary>
    HypertensiveCrisis,
}
