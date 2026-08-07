using BPTracker.Domain.Readings;

namespace BPTracker.Presentation.Readings;

/// <summary>
/// Live validation state for the two-number entry screen.
/// </summary>
/// <param name="IsValid">Whether the pair can be saved.</param>
/// <param name="Message">Why it cannot be saved, or <see langword="null"/> when valid.</param>
/// <param name="Category">The category preview, or <see langword="null"/> when invalid.</param>
public readonly record struct ReadingEntryValidation(
    bool IsValid,
    string? Message,
    BloodPressureCategory? Category)
{
    /// <summary>
    /// Validates a systolic/diastolic pair without throwing, for use on every keystroke.
    /// </summary>
    public static ReadingEntryValidation Validate(int systolic, int diastolic)
    {
        if (!SystolicPressure.TryFrom(systolic, out var upper))
        {
            return Invalid(
                $"Systolic must be between {SystolicPressure.Minimum} and {SystolicPressure.Maximum}.");
        }

        if (!DiastolicPressure.TryFrom(diastolic, out var lower))
        {
            return Invalid(
                $"Diastolic must be between {DiastolicPressure.Minimum} and {DiastolicPressure.Maximum}.");
        }

        if (upper.MmHg <= lower.MmHg)
        {
            return Invalid("Systolic must be higher than diastolic.");
        }

        return new ReadingEntryValidation(true, null, BloodPressureClassifier.Classify(upper, lower));
    }

    private static ReadingEntryValidation Invalid(string message) => new(false, message, null);
}
