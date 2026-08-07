using BPTracker.Application.Abstractions;
using BPTracker.Domain.Readings;

namespace BPTracker.Application.Readings;

/// <summary>
/// Records a new blood pressure reading.
/// </summary>
public sealed class AddReadingUseCase(IReadingRepository repository, IClock clock)
{
    private readonly IReadingRepository _repository = repository
        ?? throw new ArgumentNullException(nameof(repository));

    private readonly IClock _clock = clock
        ?? throw new ArgumentNullException(nameof(clock));

    /// <summary>Validates and stores the reading, returning the persisted entity.</summary>
    /// <exception cref="ArgumentOutOfRangeException">A pressure is outside the plausible range.</exception>
    /// <exception cref="ArgumentException">Systolic is not greater than diastolic.</exception>
    public async Task<BloodPressureReading> ExecuteAsync(
        AddReadingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var reading = BloodPressureReading.Create(
            SystolicPressure.From(request.Systolic),
            DiastolicPressure.From(request.Diastolic),
            request.MeasuredAt ?? _clock.LocalNow,
            _clock.UtcNow,
            request.Context);

        await _repository.UpsertAsync(reading, cancellationToken).ConfigureAwait(false);
        return reading;
    }
}
