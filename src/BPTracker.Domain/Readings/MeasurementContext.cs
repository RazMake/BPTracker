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
    /// <summary>Maximum length of <see cref="Tag"/>.</summary>
    /// <remarks>
    /// Short on purpose. A tag is a word or two explaining an outlier, not a diary entry, and it
    /// has to fit on one line on a phone.
    /// </remarks>
    public const int MaxTagLength = 100;

    /// <summary>Nothing recorded beyond the two pressures.</summary>
    public static MeasurementContext None => default;

    /// <summary>Which arm the cuff was on.</summary>
    public MeasurementArm Arm { get; init; }

    /// <summary>Posture during measurement.</summary>
    public BodyPosition Position { get; init; }

    /// <summary>Optional short free-text tag, such as "after a run".</summary>
    public string? Tag { get; init; }

    /// <summary>
    /// Returns a copy with the tag trimmed, and empty tags collapsed to <see langword="null"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The tag exceeds <see cref="MaxTagLength"/>.</exception>
    public MeasurementContext Normalised()
    {
        if (string.IsNullOrWhiteSpace(Tag))
        {
            return this with { Tag = null };
        }

        var trimmed = Tag.Trim();
        if (trimmed.Length > MaxTagLength)
        {
            throw new InvalidOperationException(
                $"Tag must be {MaxTagLength} characters or fewer, but was {trimmed.Length}.");
        }

        return this with { Tag = trimmed };
    }

    /// <summary>Shortens a tag to <see cref="MaxTagLength"/> rather than rejecting it.</summary>
    /// <remarks>
    /// For tags arriving from a journal file, which may predate the current limit or have been
    /// hand-edited. Losing the tail of a tag is better than losing the reading.
    /// </remarks>
    public static string? Clamp(string? tag) =>
        tag is not null && tag.Length > MaxTagLength ? tag[..MaxTagLength] : tag;
}
