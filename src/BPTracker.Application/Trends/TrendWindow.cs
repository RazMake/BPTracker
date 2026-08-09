namespace BPTracker.Application.Trends;

/// <summary>A range of measurement time the trend chart covers, inclusive at both ends.</summary>
/// <param name="From">The oldest instant in the window.</param>
/// <param name="To">The newest instant in the window.</param>
public readonly record struct TrendWindow(DateTimeOffset From, DateTimeOffset To)
{
    /// <summary>How much time the window covers.</summary>
    public TimeSpan Length => To - From;
}
