namespace BPTracker.Domain.Readings;

/// <summary>
/// A single blood pressure measurement. This is the aggregate root of the domain.
/// </summary>
/// <remarks>
/// <para>
/// Instances are immutable. Edits produce a new instance via the <c>With*</c> methods, which keeps
/// the type trivially thread safe and makes the append-only sync journal natural to implement.
/// </para>
/// <para>
/// <see cref="UpdatedAtUtc"/> and <see cref="IsDeleted"/> are domain concepts, not persistence
/// artefacts: the user can correct or retract a reading, and the sync journal needs to know which of
/// two divergent copies is newer.
/// </para>
/// </remarks>
public sealed record BloodPressureReading
{
    private BloodPressureReading()
    {
    }

    /// <summary>Stable identity, shared across devices. UUIDv7 so it sorts by creation time.</summary>
    public Guid Id { get; init; }

    /// <summary>Systolic (upper) pressure.</summary>
    public SystolicPressure Systolic { get; init; }

    /// <summary>Diastolic (lower) pressure.</summary>
    public DiastolicPressure Diastolic { get; init; }

    /// <summary>When the measurement was taken, with the offset of the place it was taken.</summary>
    public DateTimeOffset MeasuredAt { get; init; }

    /// <summary>Optional circumstances of the measurement.</summary>
    public MeasurementContext Context { get; init; }

    /// <summary>When this record was last changed. Drives last-writer-wins during sync.</summary>
    public DateTimeOffset UpdatedAtUtc { get; init; }

    /// <summary>Whether the user retracted this reading. Soft delete, so the tombstone can sync.</summary>
    public bool IsDeleted { get; init; }

    /// <summary>The informational ACC/AHA category for this reading.</summary>
    public BloodPressureCategory Category => BloodPressureClassifier.Classify(Systolic, Diastolic);

    /// <summary>Difference between systolic and diastolic pressure, in mmHg.</summary>
    public int PulsePressure => Systolic.MmHg - Diastolic.MmHg;

    /// <summary>Estimated mean arterial pressure, in mmHg.</summary>
    public double MeanArterialPressure => Diastolic.MmHg + (PulsePressure / 3.0);

    /// <summary>
    /// Creates a new reading.
    /// </summary>
    /// <param name="systolic">Systolic pressure. Must exceed <paramref name="diastolic"/>.</param>
    /// <param name="diastolic">Diastolic pressure.</param>
    /// <param name="measuredAt">When the measurement was taken.</param>
    /// <param name="nowUtc">Current time, supplied by the caller so the domain stays testable.</param>
    /// <param name="context">Optional circumstances of the measurement.</param>
    /// <param name="id">Explicit identity. Supply only when rehydrating an existing reading.</param>
    /// <exception cref="ArgumentException">Systolic is not greater than diastolic.</exception>
    public static BloodPressureReading Create(
        SystolicPressure systolic,
        DiastolicPressure diastolic,
        DateTimeOffset measuredAt,
        DateTimeOffset nowUtc,
        MeasurementContext context = default,
        Guid? id = null)
    {
        if (systolic.MmHg <= diastolic.MmHg)
        {
            throw new ArgumentException(
                $"Systolic ({systolic.MmHg}) must be greater than diastolic ({diastolic.MmHg}).",
                nameof(systolic));
        }

        return new BloodPressureReading
        {
            Id = id ?? Guid.CreateVersion7(),
            Systolic = systolic,
            Diastolic = diastolic,
            MeasuredAt = measuredAt,
            Context = context.Normalised(),
            UpdatedAtUtc = nowUtc,
            IsDeleted = false,
        };
    }

    /// <summary>Returns a copy with a replaced measurement context.</summary>
    public BloodPressureReading WithContext(MeasurementContext context, DateTimeOffset nowUtc) =>
        this with { Context = context.Normalised(), UpdatedAtUtc = nowUtc };

    /// <summary>Returns a copy marked as retracted.</summary>
    public BloodPressureReading Retract(DateTimeOffset nowUtc) =>
        this with { IsDeleted = true, UpdatedAtUtc = nowUtc };

    /// <summary>
    /// Picks the winning copy when two devices hold divergent versions of the same reading.
    /// </summary>
    /// <exception cref="ArgumentException">The two copies are not the same reading.</exception>
    public static BloodPressureReading ResolveConflict(BloodPressureReading left, BloodPressureReading right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (left.Id != right.Id)
        {
            throw new ArgumentException("Cannot merge readings with different identities.", nameof(right));
        }

        // Ties favour the retraction: resurrecting a deleted reading is worse than losing an edit.
        if (left.UpdatedAtUtc == right.UpdatedAtUtc)
        {
            return left.IsDeleted ? left : right;
        }

        return left.UpdatedAtUtc > right.UpdatedAtUtc ? left : right;
    }
}
