namespace BPTracker.Application.Trends;

/// <summary>
/// A selectable window for the trend chart.
/// </summary>
public enum TrendPeriod
{
    /// <summary>Trailing 7 days.</summary>
    Week,

    /// <summary>Trailing 30 days.</summary>
    Month,

    /// <summary>Trailing 90 days.</summary>
    Quarter,

    /// <summary>Trailing 365 days.</summary>
    Year,

    /// <summary>Everything on record.</summary>
    All,
}

/// <summary>Maps a <see cref="TrendPeriod"/> onto a concrete start instant.</summary>
public static class TrendPeriodExtensions
{
    /// <summary>Returns the inclusive start of the window ending at <paramref name="endingAt"/>.</summary>
    public static DateTimeOffset StartOf(this TrendPeriod period, DateTimeOffset endingAt) => period switch
    {
        TrendPeriod.Week => endingAt.AddDays(-7),
        TrendPeriod.Month => endingAt.AddDays(-30),
        TrendPeriod.Quarter => endingAt.AddDays(-90),
        TrendPeriod.Year => endingAt.AddDays(-365),
        TrendPeriod.All => DateTimeOffset.MinValue,
        _ => throw new ArgumentOutOfRangeException(nameof(period), period, "Unknown trend period."),
    };
}
