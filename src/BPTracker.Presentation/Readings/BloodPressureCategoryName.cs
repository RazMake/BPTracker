using BPTracker.Domain.Readings;

namespace BPTracker.Presentation.Readings;

/// <summary>
/// The words a user sees for a <see cref="BloodPressureCategory"/>.
/// </summary>
/// <remarks>
/// Shared so the phone and the desktop cannot drift apart, and so neither has to rely on the
/// enum's <c>ToString</c>, which runs the words together. The wording matches the category table
/// in the domain glossary, and stays purely descriptive: a category is never advice.
/// </remarks>
public static class BloodPressureCategoryName
{
    /// <summary>Returns the display name, or an empty string for an unrecognised value.</summary>
    public static string For(BloodPressureCategory category) => category switch
    {
        BloodPressureCategory.Hypotension => "Hypotension",
        BloodPressureCategory.Normal => "Normal",
        BloodPressureCategory.Elevated => "Elevated",
        BloodPressureCategory.HypertensionStage1 => "Hypertension stage 1",
        BloodPressureCategory.HypertensionStage2 => "Hypertension stage 2",
        BloodPressureCategory.HypertensiveCrisis => "Hypertensive crisis",
        _ => string.Empty,
    };

    /// <summary>Returns the display name, or an empty string when there is no category yet.</summary>
    public static string For(BloodPressureCategory? category) =>
        category is null ? string.Empty : For(category.Value);
}
