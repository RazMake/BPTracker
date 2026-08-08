using BPTracker.Application.Abstractions;
using BPTracker.Domain.Readings;

namespace BPTracker.Application.Readings;

/// <summary>
/// Returns recorded readings for the history list, newest first.
/// </summary>
public sealed class GetReadingHistoryUseCase(IReadingRepository repository, IClock clock)
{
    private readonly IReadingRepository _repository = repository
        ?? throw new ArgumentNullException(nameof(repository));

    private readonly IClock _clock = clock
        ?? throw new ArgumentNullException(nameof(clock));

    /// <summary>Returns readings measured within the trailing <paramref name="days"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="days"/> is below one.</exception>
    public async Task<IReadOnlyList<BloodPressureReading>> ExecuteAsync(
        int days,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(days, 1);

        var to = _clock.LocalNow;
        var readings = await _repository
            .GetRangeAsync(to.AddDays(-days), to, cancellationToken)
            .ConfigureAwait(false);

        return [.. readings.OrderByDescending(reading => reading.MeasuredAt)];
    }
}
