using BPTracker.Application.Abstractions;

namespace BPTracker.Infrastructure.Time;

/// <summary>
/// The real system clock.
/// </summary>
public sealed class SystemClock : IClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public DateTimeOffset LocalNow => DateTimeOffset.Now;
}
