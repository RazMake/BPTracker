using BPTracker.Application.Abstractions;
using BPTracker.Domain.Readings;

namespace BPTracker.Application.Readings;

/// <summary>
/// Retracts a previously recorded reading. The tombstone is kept so the deletion can sync.
/// </summary>
public sealed class RetractReadingUseCase(IReadingRepository repository, IClock clock)
{
    private readonly IReadingRepository _repository = repository
        ?? throw new ArgumentNullException(nameof(repository));

    private readonly IClock _clock = clock
        ?? throw new ArgumentNullException(nameof(clock));

    /// <summary>
    /// Marks the reading as retracted.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if a reading was retracted, <see langword="false"/> if the id is unknown.
    /// </returns>
    public async Task<bool> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.FindAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return false;
        }

        var retracted = existing.Retract(_clock.UtcNow);
        await _repository.UpsertAsync(retracted, cancellationToken).ConfigureAwait(false);
        return true;
    }
}
