using System.Globalization;
using BPTracker.Domain.Readings;

namespace BPTracker.Presentation.Charts;

/// <summary>
/// Builds the chart's reference lines and date ticks.
/// </summary>
public static class ChartAxisBuilder
{
    /// <summary>Smallest gap allowed between two date ticks, in pixels.</summary>
    private const double MinimumLabelGap = 76d;

    /// <summary>Stops a pathological zoom from generating ticks forever.</summary>
    private const int MaximumLabels = 64;

    /// <summary>
    /// Horizontal lines every <see cref="ChartValueBounds.Step"/> mmHg, plus the four healthy band
    /// edges so the reader can see the target without counting gridlines.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="scale"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<ChartGridLine> GridLines(ChartScale scale)
    {
        ArgumentNullException.ThrowIfNull(scale);

        var edges = new HashSet<int>
        {
            HealthyRange.Diastolic.Lowest,
            HealthyRange.Diastolic.TooHigh,
            HealthyRange.Systolic.Lowest,
            HealthyRange.Systolic.TooHigh,
        };

        var values = new SortedSet<int>(edges.Where(edge => edge > scale.Lowest && edge < scale.Highest));
        for (var value = scale.Lowest; value <= scale.Highest; value += ChartValueBounds.Step)
        {
            values.Add(value);
        }

        return [.. values.Select(value => new ChartGridLine(scale.Y(value), value, edges.Contains(value)))];
    }

    /// <summary>Date ticks across the visible window, spaced so the labels cannot collide.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="scale"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<ChartTimeLabel> TimeLabels(ChartScale scale, double plotWidth)
    {
        ArgumentNullException.ThrowIfNull(scale);

        var stepDays = StepDays(scale.PixelsPerHour);
        var day = FirstTick(scale, stepDays);
        var labels = new List<ChartTimeLabel>();

        for (var index = 0; index < MaximumLabels; index++)
        {
            var x = scale.X(day);
            if (x > plotWidth)
            {
                break;
            }

            if (x >= 0)
            {
                labels.Add(new ChartTimeLabel(x, Format(day, stepDays)));
            }

            day = day.AddDays(stepDays);
        }

        return labels;
    }

    private static int StepDays(double pixelsPerHour) =>
        Math.Max(1, (int)Math.Ceiling(MinimumLabelGap / (pixelsPerHour * 24)));

    // Ticks are anchored to the origin rather than to the left edge, so they stay put while scrolling.
    private static DateTimeOffset FirstTick(ChartScale scale, int stepDays)
    {
        var anchor = new DateTimeOffset(scale.Origin.Date, scale.Origin.Offset);
        var elapsedDays = (scale.TimeAt(0) - anchor).TotalDays;
        return anchor.AddDays(Math.Floor(elapsedDays / stepDays) * stepDays);
    }

    private static string Format(DateTimeOffset day, int stepDays) =>
        day.ToString(stepDays >= 28 ? "MMM yyyy" : "d MMM", CultureInfo.CurrentCulture);
}
