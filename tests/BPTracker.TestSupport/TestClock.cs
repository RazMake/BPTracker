using BPTracker.Application.Abstractions;

namespace BPTracker.TestSupport;

/// <summary>
/// A clock the test controls. Never returns a real time, so tests stay deterministic.
/// </summary>
public sealed class TestClock(DateTimeOffset utcNow) : IClock
{
    /// <summary>A fixed, arbitrary instant used as the default "now" across the test suite.</summary>
    public static readonly DateTimeOffset DefaultNow =
        new(2026, 3, 15, 8, 30, 0, TimeSpan.Zero);

    /// <summary>Creates a clock at <see cref="DefaultNow"/>.</summary>
    public TestClock()
        : this(DefaultNow)
    {
    }

    /// <inheritdoc />
    public DateTimeOffset UtcNow { get; private set; } = utcNow;

    /// <inheritdoc />
    public DateTimeOffset LocalNow => UtcNow;

    /// <summary>Moves the clock forward.</summary>
    public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);
}
