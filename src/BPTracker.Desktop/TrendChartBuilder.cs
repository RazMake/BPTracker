using System.Globalization;
using BPTracker.Presentation.Trends;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace BPTracker.Desktop;

/// <summary>
/// Adapts the shared trend view model onto LiveCharts types.
/// Lives in the UI head so the charting package never leaks into shared code.
/// </summary>
internal static class TrendChartBuilder
{
    private static readonly SKColor SystolicColour = new(214, 69, 69);
    private static readonly SKColor DiastolicColour = new(45, 108, 223);
    private static readonly SKColor SmoothedColour = new(107, 122, 136);
    private static readonly SKColor MutedColour = new(107, 122, 136);
    private static readonly SKColor GridColour = new(221, 227, 233);

    internal static ISeries[] BuildSeries(TrendViewModel trend) =>
    [
        Line("Systolic", [.. trend.Daily.Select(point => point.AverageSystolic)], SystolicColour, 4),
        Line("Diastolic", [.. trend.Daily.Select(point => point.AverageDiastolic)], DiastolicColour, 4),
        Line("Systolic trend", [.. trend.Smoothed.Select(point => point.AverageSystolic)], SmoothedColour, 0),
    ];

    internal static Axis[] BuildXAxes(TrendViewModel trend) =>
    [
        new Axis
        {
            Labels = [.. trend.Daily.Select(point =>
                point.Day.ToString("dd MMM", CultureInfo.CurrentCulture))],
            LabelsRotation = 45,
            LabelsPaint = new SolidColorPaint(MutedColour),
            TextSize = 11,
        },
    ];

    internal static Axis[] BuildYAxes() =>
    [
        new Axis
        {
            Name = "mmHg",
            MinLimit = 40,
            MaxLimit = 200,
            LabelsPaint = new SolidColorPaint(MutedColour),
            SeparatorsPaint = new SolidColorPaint(GridColour) { StrokeThickness = 1 },
            TextSize = 11,
        },
    ];

    private static LineSeries<double> Line(string name, double[] values, SKColor colour, float pointSize) =>
        new()
        {
            Name = name,
            Values = values,
            Stroke = new SolidColorPaint(colour) { StrokeThickness = 2 },
            GeometryStroke = new SolidColorPaint(colour) { StrokeThickness = 2 },
            GeometryFill = new SolidColorPaint(SKColors.White),
            GeometrySize = pointSize,
            Fill = null,
            LineSmoothness = 0.2,
        };
}
