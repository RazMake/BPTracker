using System.Globalization;
using BPTracker.Domain.Readings;
using BPTracker.Presentation.Charts;
using BPTracker.Presentation.Trends;
using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WPF;
using SkiaSharp;

namespace BPTracker.Desktop;

/// <summary>
/// Adapts the shared trend view model onto LiveCharts types.
/// Lives in the UI head so the charting package never leaks into shared code.
/// </summary>
internal static class TrendChartBuilder
{
    private const int AxisLowest = 40;
    private const int AxisHighest = 200;

    // The phone's palette, in Resources\Styles\Colors.xaml. Change one, change both.
    private static readonly SKColor SystolicColour = new(0x7E, 0xC8, 0xFF);
    private static readonly SKColor DiastolicColour = new(0xB7, 0x9C, 0xFF);
    private static readonly SKColor CrisisColour = new(0xFF, 0x2D, 0x3E);
    private static readonly SKColor TaggedColour = new(0xFF, 0xD1, 0x66);
    private static readonly SKColor SmoothedColour = new(0x8A, 0x98, 0xA6);
    private static readonly SKColor MutedColour = new(0x8A, 0x98, 0xA6);
    private static readonly SKColor GridColour = new(0x1C, 0x24, 0x2D);
    private static readonly SKColor SurfaceColour = new(0x18, 0x1F, 0x27);
    private static readonly SKColor InkColour = new(0xE9, 0xEE, 0xF3);
    private static readonly SKColor NormalBandColour = new(0x3F, 0xBF, 0x77, 0x1A);
    private static readonly SKColor NormalBandLabelColour = new(0x3F, 0xBF, 0x77, 0x7A);

    /// <summary>Puts the chart's own chrome, which is drawn by SkiaSharp, onto the dark palette.</summary>
    internal static void ApplyTheme(CartesianChart chart)
    {
        ArgumentNullException.ThrowIfNull(chart);

        chart.TooltipBackgroundPaint = new SolidColorPaint(SurfaceColour);
        chart.TooltipTextPaint = new SolidColorPaint(InkColour);
        chart.TooltipTextSize = 12;
        chart.LegendBackgroundPaint = new SolidColorPaint(SurfaceColour);
        chart.LegendTextPaint = new SolidColorPaint(MutedColour);
        chart.LegendTextSize = 12;
    }

    internal static ISeries[] BuildSeries(TrendViewModel trend)    {
        var systolic = trend.Daily.Select(point => point.AverageSystolic).ToArray();
        var diastolic = trend.Daily.Select(point => point.AverageDiastolic).ToArray();

        return
        [
            Line("Systolic", systolic, SystolicColour, 4),
            Line("Diastolic", diastolic, DiastolicColour, 4),
            Line("Systolic trend", [.. trend.Smoothed.Select(point => point.AverageSystolic)], SmoothedColour, 0),
            CrisisOverlay("Systolic crisis", systolic, CrisisThreshold.Systolic),
            CrisisOverlay("Diastolic crisis", diastolic, CrisisThreshold.Diastolic),
            TagMarkers(trend),
        ];
    }

    /// <summary>Shades the same labelled normal corridors the phone draws behind its lines.</summary>
    internal static RectangularSection[] BuildSections() =>
    [
        .. PressureBands.For(AxisLowest, AxisHighest).Select(band => new RectangularSection
        {
            Yi = band.Lowest,
            Yj = band.Highest,
            Fill = new SolidColorPaint(NormalBandColour),
            Label = band.Label,
            LabelPaint = new SolidColorPaint(NormalBandLabelColour),
            LabelSize = 11,
        }),
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
            NamePaint = new SolidColorPaint(MutedColour),
            MinLimit = AxisLowest,
            MaxLimit = AxisHighest,
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
            Stroke = new SolidColorPaint(colour) { StrokeThickness = 2.5f },
            GeometryStroke = new SolidColorPaint(colour) { StrokeThickness = 2.5f },
            GeometryFill = new SolidColorPaint(SurfaceColour),
            GeometrySize = pointSize,
            Fill = null,
            LineSmoothness = 0,
        };

    // LiveCharts paints a series in one colour, so the crisis stretch is a second series laid over
    // the first with everything below the threshold left null, which draws as a gap.
    private static LineSeries<double?> CrisisOverlay(string name, double[] values, int threshold) =>
        new()
        {
            Name = name,
            Values = [.. values.Select(value => value >= threshold ? value : (double?)null)],
            Stroke = new SolidColorPaint(CrisisColour) { StrokeThickness = 3 },
            GeometryStroke = new SolidColorPaint(CrisisColour) { StrokeThickness = 3 },
            GeometryFill = new SolidColorPaint(CrisisColour),
            GeometrySize = 6,
            Fill = null,
            LineSmoothness = 0,
            IsVisibleAtLegend = false,
        };

    private static ScatterSeries<TagPoint> TagMarkers(TrendViewModel trend) =>
        new()
        {
            Name = "Tagged",
            Values = [.. trend.Daily
                .Select((point, index) => new TagPoint(index, point.AverageSystolic, point.Tag))
                .Where(point => point.Tag is not null)],
            Mapping = (point, index) => new(point.X, point.Y),
            Fill = new SolidColorPaint(TaggedColour),
            GeometrySize = 14,
            YToolTipLabelFormatter = point => point.Model?.Tag ?? string.Empty,
        };

    private sealed record TagPoint(double X, double Y, string? Tag);
}
