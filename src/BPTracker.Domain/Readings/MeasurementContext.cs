namespace BPTracker.Domain.Readings;

/// <summary>
/// The optional circumstances of a measurement, grouped so they travel together.
/// </summary>
/// <remarks>
/// <see langword="default"/> means "nothing recorded", which is the common case on the phone
/// where the priority is entering two numbers as fast as possible.
/// </remarks>
public readonly record struct MeasurementContext
{
    /// <summary>Maximum length of <see cref="Note"/>.</summary>
    public const int MaxNoteLength = 500;

    /// <summary>Nothing recorded beyond the two pressures.</summary>
    public static MeasurementContext None => default;

    /// <summary>Which arm the cuff was on.</summary>
    public MeasurementArm Arm { get; init; }

    /// <summary>Posture during measurement.</summary>
    public BodyPosition Position { get; init; }

    /// <summary>Optional free-text note.</summary>
    public string? Note { get; init; }

    /// <summary>
    /// Returns a copy with the note trimmed, and empty notes collapsed to <see langword="null"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The note exceeds <see cref="MaxNoteLength"/>.</exception>
    public MeasurementContext Normalised()
    {
        if (string.IsNullOrWhiteSpace(Note))
        {
            return this with { Note = null };
        }

        var trimmed = Note.Trim();
        if (trimmed.Length > MaxNoteLength)
        {
            throw new InvalidOperationException(
                $"Note must be {MaxNoteLength} characters or fewer, but was {trimmed.Length}.");
        }

        return this with { Note = trimmed };
    }
}
