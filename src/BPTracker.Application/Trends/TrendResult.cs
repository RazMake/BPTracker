using BPTracker.Domain.Trends;

namespace BPTracker.Application.Trends;

/// <summary>Headline numbers shown alongside the trend chart.</summary>
/// <param name="AverageSystolic">Mean systolic pressure over the window.</param>
/// <param name="AverageDiastolic">Mean diastolic pressure over the window.</param>
/// <param name="HighestSystolic">Highest systolic pressure over the window.</param>
/// <param name="LowestSystolic">Lowest systolic pressure over the window.</param>
/// <param name="ReadingCount">Number of readings in the window.</param>
public readonly record struct TrendSummary(
    double AverageSystolic,
    double AverageDiastolic,
    int HighestSystolic,
    int LowestSystolic,
    int ReadingCount)
{
    /// <summary>The summary for a window containing no readings.</summary>
    public static TrendSummary Empty => default;

    /// <summary>Whether the window contained any readings.</summary>
    public bool HasData => ReadingCount > 0;
}

/// <summary>Everything the trend screen needs for one window.</summary>
/// <param name="Daily">One point per day, oldest first.</param>
/// <param name="Smoothed">The moving average of <paramref name="Daily"/>.</param>
/// <param name="Summary">Headline statistics.</param>
public sealed record TrendResult(
    IReadOnlyList<TrendPoint> Daily,
    IReadOnlyList<TrendPoint> Smoothed,
    TrendSummary Summary);
