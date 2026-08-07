namespace BPTracker.Domain.Trends;

/// <summary>
/// One point on the trend chart: the aggregated readings for a single calendar day.
/// </summary>
/// <param name="Day">The local calendar day, at midnight.</param>
/// <param name="AverageSystolic">Mean systolic pressure for the day.</param>
/// <param name="AverageDiastolic">Mean diastolic pressure for the day.</param>
/// <param name="ReadingCount">How many readings were averaged.</param>
/// <param name="Tag">The day's tags joined together, or <see langword="null"/> if none were written.</param>
public readonly record struct TrendPoint(
    DateOnly Day,
    double AverageSystolic,
    double AverageDiastolic,
    int ReadingCount,
    string? Tag = null);
