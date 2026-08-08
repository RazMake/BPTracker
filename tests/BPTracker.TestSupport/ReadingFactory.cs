using BPTracker.Domain.Readings;

namespace BPTracker.TestSupport;

/// <summary>
/// Builds readings for tests without repeating the ceremony at every call site.
/// </summary>
public static class ReadingFactory
{
    /// <summary>Creates a reading with sensible defaults, overriding only what the test cares about.</summary>
    public static BloodPressureReading Create(
        int systolic = 120,
        int diastolic = 80,
        DateTimeOffset? measuredAt = null,
        DateTimeOffset? updatedAtUtc = null,
        Guid? id = null,
        string? tag = null) =>
        BloodPressureReading.Create(
            SystolicPressure.From(systolic),
            DiastolicPressure.From(diastolic),
            measuredAt ?? TestClock.DefaultNow,
            updatedAtUtc ?? TestClock.DefaultNow,
            new MeasurementContext { Tag = tag },
            id);

    /// <summary>Creates a run of readings, one per day, ending at <see cref="TestClock.DefaultNow"/>.</summary>
    public static IReadOnlyList<BloodPressureReading> CreateDailySeries(int days, int systolic = 120)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(days, 1);

        return [.. Enumerable.Range(0, days).Select(offset => Create(
            systolic + offset,
            80,
            TestClock.DefaultNow.AddDays(-offset)))];
    }
}
