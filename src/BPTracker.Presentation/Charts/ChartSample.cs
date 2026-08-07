using BPTracker.Domain.Readings;

namespace BPTracker.Presentation.Charts;

/// <summary>
/// One measurement, reduced to what a chart needs to plot it.
/// </summary>
/// <param name="MeasuredAt">When the measurement was taken.</param>
/// <param name="Systolic">Systolic pressure in mmHg.</param>
/// <param name="Diastolic">Diastolic pressure in mmHg.</param>
/// <param name="Tag">The reading's tag, if it has one.</param>
public readonly record struct ChartSample(
    DateTimeOffset MeasuredAt,
    int Systolic,
    int Diastolic,
    string? Tag = null)
{
    /// <summary>Projects a reading onto the chart.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="reading"/> is <see langword="null"/>.</exception>
    public static ChartSample From(BloodPressureReading reading)
    {
        ArgumentNullException.ThrowIfNull(reading);

        return new ChartSample(
            reading.MeasuredAt,
            reading.Systolic.MmHg,
            reading.Diastolic.MmHg,
            reading.Context.Tag);
    }
}
