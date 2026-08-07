namespace BPTracker.Application.Abstractions;

/// <summary>
/// Supplies the current time. Injected so time-dependent behaviour stays deterministic in tests.
/// </summary>
public interface IClock
{
    /// <summary>The current instant, in UTC.</summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>The current instant, with the device's local offset.</summary>
    DateTimeOffset LocalNow { get; }
}
