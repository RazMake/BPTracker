using BPTracker.Domain.Readings;

namespace BPTracker.Application.Abstractions;

/// <summary>
/// Persistence port for <see cref="BloodPressureReading"/>. Implemented in the infrastructure layer.
/// </summary>
public interface IReadingRepository
{
    /// <summary>Inserts a reading, or replaces the stored copy if it already exists.</summary>
    Task UpsertAsync(BloodPressureReading reading, CancellationToken cancellationToken = default);

    /// <summary>Returns a single reading, or <see langword="null"/> if it is unknown.</summary>
    Task<BloodPressureReading?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns non-retracted readings measured within the inclusive range, newest first.
    /// </summary>
    Task<IReadOnlyList<BloodPressureReading>> GetRangeAsync(
        DateTimeOffset fromInclusive,
        DateTimeOffset toInclusive,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns every stored reading including retracted tombstones. Used by sync, not by the UI.
    /// </summary>
    Task<IReadOnlyList<BloodPressureReading>> GetAllIncludingRetractedAsync(
        CancellationToken cancellationToken = default);
}
