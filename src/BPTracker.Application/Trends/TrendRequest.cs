namespace BPTracker.Application.Trends;

/// <summary>What the trend screen is asking the use case for.</summary>
public sealed record TrendRequest
{
    /// <summary>Width of the moving average when the caller does not care.</summary>
    public const int DefaultSmoothingWindowDays = 7;

    /// <summary>How much history one page covers.</summary>
    public required TrendPeriod Period { get; init; }

    /// <summary>Which page to load. Zero is the most recent one.</summary>
    public int PageIndex { get; init; }

    /// <summary>Width of the moving average, in days.</summary>
    public int SmoothingWindowDays { get; init; } = DefaultSmoothingWindowDays;
}
