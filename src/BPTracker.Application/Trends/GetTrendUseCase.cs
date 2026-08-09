using BPTracker.Application.Abstractions;
using BPTracker.Domain.Readings;
using BPTracker.Domain.Trends;

namespace BPTracker.Application.Trends;

/// <summary>
/// Builds the smoothed daily series that the desktop trend chart renders.
/// </summary>
public sealed class GetTrendUseCase(IReadingRepository repository, IClock clock)
{
    private readonly IReadingRepository _repository = repository
        ?? throw new ArgumentNullException(nameof(repository));

    private readonly IClock _clock = clock
        ?? throw new ArgumentNullException(nameof(clock));

    /// <summary>
    /// Returns the daily averages and their moving average for one page of the chosen period.
    /// </summary>
    /// <param name="request">Which page of which period to load.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<TrendResult> ExecuteAsync(
        TrendRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.SmoothingWindowDays, 1);

        var window = request.Period.Page(_clock.LocalNow, request.PageIndex);

        var readings = await _repository
            .GetRangeAsync(window.From, window.To, cancellationToken)
            .ConfigureAwait(false);

        var earliest = await _repository
            .GetEarliestMeasuredAtAsync(cancellationToken)
            .ConfigureAwait(false);

        var chronologicalReadings = readings.OrderBy(reading => reading.MeasuredAt).ToArray();
        var daily = TrendCalculator.DailyAverages(chronologicalReadings);
        var smoothed = TrendCalculator.MovingAverage(daily, request.SmoothingWindowDays);

        return new TrendResult(
            chronologicalReadings,
            daily,
            smoothed,
            Summarise(readings),
            window,
            earliest);
    }

    private static TrendSummary Summarise(IReadOnlyList<BloodPressureReading> readings)
    {
        if (readings.Count == 0)
        {
            return TrendSummary.Empty;
        }

        return new TrendSummary(
            readings.Average(reading => (double)reading.Systolic.MmHg),
            readings.Average(reading => (double)reading.Diastolic.MmHg),
            readings.Max(reading => reading.Systolic.MmHg),
            readings.Min(reading => reading.Systolic.MmHg),
            readings.Count);
    }
}
