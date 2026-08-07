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
    /// Returns the daily averages and their moving average for the trailing window.
    /// </summary>
    /// <param name="period">How far back to look.</param>
    /// <param name="smoothingWindowDays">Width of the moving average, in days.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<TrendResult> ExecuteAsync(
        TrendPeriod period,
        int smoothingWindowDays = 7,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(smoothingWindowDays, 1);

        var to = _clock.LocalNow;
        var from = period.StartOf(to);

        var readings = await _repository
            .GetRangeAsync(from, to, cancellationToken)
            .ConfigureAwait(false);

        var daily = TrendCalculator.DailyAverages(readings);
        var smoothed = TrendCalculator.MovingAverage(daily, smoothingWindowDays);

        return new TrendResult(daily, smoothed, Summarise(readings));
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
