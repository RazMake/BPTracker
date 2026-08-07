using BPTracker.Domain.Readings;

namespace BPTracker.Domain.Trends;

/// <summary>
/// Turns raw readings into the series the desktop trend chart draws.
/// </summary>
/// <remarks>
/// Pure functions with no I/O, so the charting behaviour is fully unit testable.
/// </remarks>
public static class TrendCalculator
{
    /// <summary>
    /// Collapses readings into one point per local calendar day, ordered oldest first.
    /// Retracted readings are ignored.
    /// </summary>
    public static IReadOnlyList<TrendPoint> DailyAverages(IEnumerable<BloodPressureReading> readings)
    {
        ArgumentNullException.ThrowIfNull(readings);

        return [.. readings
            .Where(reading => !reading.IsDeleted)
            .GroupBy(reading => DateOnly.FromDateTime(reading.MeasuredAt.LocalDateTime))
            .OrderBy(group => group.Key)
            .Select(group => new TrendPoint(
                group.Key,
                group.Average(reading => (double)reading.Systolic.MmHg),
                group.Average(reading => (double)reading.Diastolic.MmHg),
                group.Count(),
                JoinTags(group)))];
    }

    private static string? JoinTags(IEnumerable<BloodPressureReading> readings)
    {
        var tags = readings
            .Select(reading => reading.Context.Tag)
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .ToArray();

        return tags.Length == 0 ? null : string.Join("; ", tags);
    }

    /// <summary>
    /// Smooths a daily series with a trailing simple moving average.
    /// </summary>
    /// <param name="points">Daily points, oldest first.</param>
    /// <param name="windowDays">Number of points to average over. Must be at least one.</param>
    /// <remarks>
    /// The leading points average over fewer samples rather than being dropped, so the smoothed
    /// series always lines up with the raw series on the chart's x-axis.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="windowDays"/> is below one.</exception>
    public static IReadOnlyList<TrendPoint> MovingAverage(IReadOnlyList<TrendPoint> points, int windowDays)
    {
        ArgumentNullException.ThrowIfNull(points);
        ArgumentOutOfRangeException.ThrowIfLessThan(windowDays, 1);

        var smoothed = new List<TrendPoint>(points.Count);

        for (var index = 0; index < points.Count; index++)
        {
            var start = Math.Max(0, index - windowDays + 1);
            var count = index - start + 1;

            var systolicSum = 0d;
            var diastolicSum = 0d;
            var readingSum = 0;

            for (var offset = start; offset <= index; offset++)
            {
                systolicSum += points[offset].AverageSystolic;
                diastolicSum += points[offset].AverageDiastolic;
                readingSum += points[offset].ReadingCount;
            }

            smoothed.Add(new TrendPoint(
                points[index].Day,
                systolicSum / count,
                diastolicSum / count,
                readingSum));
        }

        return smoothed;
    }
}
