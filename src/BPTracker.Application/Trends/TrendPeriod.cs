namespace BPTracker.Application.Trends;

/// <summary>
/// A selectable window for the trend chart.
/// </summary>
/// <remarks>
/// A year is deliberately the longest. The chart keeps a fixed number of days on screen, so older
/// history is reached by paging rather than by squeezing more readings into the same width.
/// </remarks>
public enum TrendPeriod
{
    /// <summary>Seven days.</summary>
    Week,

    /// <summary>Thirty days.</summary>
    Month,

    /// <summary>Ninety days.</summary>
    Quarter,

    /// <summary>Three hundred and sixty five days.</summary>
    Year,
}

/// <summary>Maps a <see cref="TrendPeriod"/> onto the window one page of it covers.</summary>
public static class TrendPeriodExtensions
{
    /// <summary>How many days one page of the period covers.</summary>
    public static int Days(this TrendPeriod period) => period switch
    {
        TrendPeriod.Week => 7,
        TrendPeriod.Month => 30,
        TrendPeriod.Quarter => 90,
        TrendPeriod.Year => 365,
        _ => throw new ArgumentOutOfRangeException(nameof(period), period, "Unknown trend period."),
    };

    /// <summary>
    /// Returns the window one page covers. Page zero ends at <paramref name="endingAt"/> and each
    /// further page steps one whole window further back.
    /// </summary>
    /// <param name="period">How much history one page covers.</param>
    /// <param name="endingAt">The newest instant of page zero.</param>
    /// <param name="pageIndex">Which page, counting back from zero.</param>
    public static TrendWindow Page(this TrendPeriod period, DateTimeOffset endingAt, int pageIndex = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(pageIndex);

        var days = (double)period.Days();
        var to = endingAt.AddDays(-days * pageIndex);

        return new TrendWindow(to.AddDays(-days), to);
    }
}
