using System.Globalization;
using BPTracker.Domain.Readings;
using BPTracker.Domain.Trends;

namespace BPTracker.Presentation.Trends;

/// <summary>One exact reading plus the daily moving average drawn at the same chart position.</summary>
/// <param name="MeasuredAt">When the reading was taken.</param>
/// <param name="Systolic">Exact systolic value.</param>
/// <param name="Diastolic">Exact diastolic value.</param>
/// <param name="SmoothedSystolic">Daily moving-average systolic value.</param>
/// <param name="Tag">Optional reading tag.</param>
public readonly record struct TrendChartSample(
    DateTimeOffset MeasuredAt,
    double Systolic,
    double Diastolic,
    double SmoothedSystolic,
    string? Tag)
{
    /// <summary>Date and time shown as the combined tooltip header.</summary>
    public string TimeText => MeasuredAt.ToString("g", CultureInfo.CurrentCulture);

    /// <summary>Systolic value shown in the combined tooltip.</summary>
    public string SystolicText =>
        string.Create(CultureInfo.CurrentCulture, $"{Systolic:F0} mmHg");

    /// <summary>Diastolic value shown in the combined tooltip.</summary>
    public string DiastolicText =>
        string.Create(CultureInfo.CurrentCulture, $"{Diastolic:F0} mmHg");
}

/// <summary>Projects exact readings and daily smoothing onto one index-aligned chart series.</summary>
public static class TrendChartSampleBuilder
{
    /// <summary>Builds samples in the same order as <paramref name="readings"/>.</summary>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public static IReadOnlyList<TrendChartSample> Build(
        IReadOnlyList<BloodPressureReading> readings,
        IReadOnlyList<TrendPoint> smoothed)
    {
        ArgumentNullException.ThrowIfNull(readings);
        ArgumentNullException.ThrowIfNull(smoothed);

        var smoothingByDay = smoothed.ToDictionary(point => point.Day);
        var samples = new List<TrendChartSample>(readings.Count);

        foreach (var reading in readings)
        {
            var day = DateOnly.FromDateTime(reading.MeasuredAt.LocalDateTime);
            var smoothedSystolic = smoothingByDay.TryGetValue(day, out var smoothedPoint)
                ? smoothedPoint.AverageSystolic
                : reading.Systolic.MmHg;

            samples.Add(new TrendChartSample(
                reading.MeasuredAt,
                reading.Systolic.MmHg,
                reading.Diastolic.MmHg,
                smoothedSystolic,
                reading.Context.Tag));
        }

        return samples;
    }
}
