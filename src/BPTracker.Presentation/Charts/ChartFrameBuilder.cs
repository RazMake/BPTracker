using System.Globalization;
using BPTracker.Domain.Readings;

namespace BPTracker.Presentation.Charts;

/// <summary>
/// Composes a whole <see cref="ChartFrame"/> from the samples and the current view state.
/// </summary>
public static class ChartFrameBuilder
{
    /// <summary>Builds the frame, or <see cref="ChartFrame.Empty"/> when there is nothing to draw.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <see langword="null"/>.</exception>
    public static ChartFrame Build(ChartRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Samples.Count == 0 || request.PlotWidth <= 0 || request.PlotHeight <= 0)
        {
            return ChartFrame.Empty;
        }

        var bounds = ChartValueBounds.For(request.Samples);
        var scale = new ChartScale(
            request.Samples[0].MeasuredAt,
            request.PixelsPerHour,
            request.Offset,
            request.PlotHeight,
            bounds.Lowest,
            bounds.Highest);

        var systolic = ChartLineBuilder.Build(
            request.Samples, sample => sample.Systolic, CrisisThreshold.Systolic, scale);

        var diastolic = ChartLineBuilder.Build(
            request.Samples, sample => sample.Diastolic, CrisisThreshold.Diastolic, scale);

        return new ChartFrame
        {
            Bands = Shade(bounds, scale),
            Systolic = systolic,
            Diastolic = diastolic,
            GridLines = ChartAxisBuilder.GridLines(scale),
            TimeLabels = ChartAxisBuilder.TimeLabels(scale, request.PlotWidth),
            Cursor = BuildCursor(request, systolic, diastolic),
        };
    }

    private static List<ChartBandArea> Shade(ChartValueBounds bounds, ChartScale scale)
    {
        var corridors = PressureBands.For(bounds.Lowest, bounds.Highest);
        var areas = new List<ChartBandArea>(corridors.Count);

        foreach (var corridor in corridors)
        {
            areas.Add(new ChartBandArea(
                scale.Y(corridor.Highest), scale.Y(corridor.Lowest), corridor.Label));
        }

        return areas;
    }

    // The cursor snaps to a measurement: a read-out between two of them would be an invention.
    private static ChartCursor? BuildCursor(ChartRequest request, ChartLine systolic, ChartLine diastolic)
    {
        if (request.CursorX is not { } cursorX)
        {
            return null;
        }

        var index = NearestIndex(systolic.Dots, cursorX);
        var sample = request.Samples[index];

        return new ChartCursor
        {
            X = systolic.Dots[index].At.X,
            Sample = sample,
            Systolic = systolic.Dots[index].At,
            Diastolic = diastolic.Dots[index].At,
            TimeText = sample.MeasuredAt.ToString("ddd d MMM HH:mm", CultureInfo.CurrentCulture),
            ValueText = string.Create(
                CultureInfo.CurrentCulture,
                $"{sample.Systolic} / {sample.Diastolic} mmHg"),
            Tag = string.IsNullOrWhiteSpace(sample.Tag) ? null : sample.Tag,
        };
    }

    private static int NearestIndex(IReadOnlyList<ChartDot> dots, double x)
    {
        var nearest = 0;
        var shortest = double.MaxValue;

        for (var index = 0; index < dots.Count; index++)
        {
            var distance = Math.Abs(dots[index].At.X - x);
            if (distance < shortest)
            {
                shortest = distance;
                nearest = index;
            }
        }

        return nearest;
    }
}
