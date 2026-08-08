using System.Globalization;
using BPTracker.Application.Trends;
using BPTracker.Domain.Readings;
using BPTracker.Presentation.Charts;
using BPTracker.Presentation.Trends;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
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
    private static readonly SKColor CrosshairColour = new(0xD5, 0xDB, 0xE1, 0xA0);
    private static readonly SKColor SelectionColour = new(0xE9, 0xEE, 0xF3);
    private static readonly SKColor NormalBandColour = new(0x3F, 0xBF, 0x77, 0x1A);
    private static readonly SKColor NormalBandLabelColour = new(0x3F, 0xBF, 0x77, 0x7A);

    /// <summary>Puts the chart's own chrome, which is drawn by SkiaSharp, onto the dark palette.</summary>
    internal static void ApplyTheme(CartesianChart chart)
    {
        ArgumentNullException.ThrowIfNull(chart);

        chart.TooltipBackgroundPaint = new SolidColorPaint(SurfaceColour);
        chart.TooltipTextPaint = new SolidColorPaint(InkColour);
        chart.TooltipTextSize = 12;
        // CompareOnlyX (without TakeClosest) returns every point whose hover area overlaps the
        // pointer, ignoring distance. With many readings in view that is nearly the whole series,
        // so the tooltip listed dozens of systolic/diastolic values at once. TakeClosest narrows
        // each series down to the one nearest point, which is what a hover tooltip should show.
        chart.FindingStrategy = FindingStrategy.CompareOnlyXTakeClosest;
        chart.LegendBackgroundPaint = new SolidColorPaint(SurfaceColour);
        chart.LegendTextPaint = new SolidColorPaint(MutedColour);
        chart.LegendTextSize = 12;
    }

    internal static ISeries[] BuildSeries(TrendViewModel trend, BloodPressureReading? highlighted = null)
    {
        var samples = trend.ChartSamples.ToArray();

        return
        [
            Line(
                "Systolic",
                samples,
                point => point.Systolic,
                new LineAppearance(
                    SystolicColour,
                    4,
                    index => samples[index].TimeText,
                    index => samples[index].SystolicText)),
            Line(
                "Diastolic",
                samples,
                point => point.Diastolic,
                new LineAppearance(
                    DiastolicColour,
                    4,
                    index => samples[index].TimeText,
                    index => samples[index].DiastolicText)),
            Line(
                "Systolic trend",
                samples,
                point => point.SmoothedSystolic,
                new LineAppearance(SmoothedColour, 0, null, null, false)),
            .. CrisisOverlays("Systolic crisis", samples, point => point.Systolic, CrisisThreshold.Systolic),
            .. CrisisOverlays("Diastolic crisis", samples, point => point.Diastolic, CrisisThreshold.Diastolic),
            TagMarkers(samples),
            .. SelectionMarkers(highlighted),
        ];
    }

    /// <summary>Shades the same labelled normal corridors the phone draws behind its lines.</summary>
    internal static ChartValueBounds BuildBounds(TrendViewModel trend) =>
        ChartValueBounds.ForTrend(trend.ChartSamples);

    internal static RectangularSection BuildPointerGuide() => new()
    {
        Xi = 0,
        Xj = 0,
        Stroke = new SolidColorPaint(CrosshairColour) { StrokeThickness = 1.5f },
        IsVisible = false,
        ZIndex = 100,
    };

    /// <summary>A brighter, persistent counterpart to the pointer guide, driven by a table click.</summary>
    internal static RectangularSection BuildSelectionGuide() => new()
    {
        Xi = 0,
        Xj = 0,
        Stroke = new SolidColorPaint(SelectionColour) { StrokeThickness = 1.5f },
        IsVisible = false,
        ZIndex = 90,
    };

    internal static RectangularSection[] BuildSections(
        ChartValueBounds bounds,
        RectangularSection pointerGuide,
        RectangularSection selectionGuide) =>
    [
        .. PressureBands.For(bounds.Lowest, bounds.Highest).Select(band => new RectangularSection
        {
            Yi = band.Highest,
            Yj = band.Lowest,
            Fill = new SolidColorPaint(NormalBandColour),
            Label = band.Label,
            LabelPaint = new SolidColorPaint(NormalBandLabelColour),
            LabelSize = 11,
        }),
        selectionGuide,
        pointerGuide,
    ];

    internal static ICartesianAxis[] BuildXAxes(TrendViewModel trend) =>
    [
        new XamlDateTimeAxis
        {
            Interval = LabelInterval(trend.Period),
            DateFormatter = date => date.ToString("d MMM", CultureInfo.CurrentCulture),
            LabelsPaint = new SolidColorPaint(MutedColour),
            TextSize = 10,
        },
    ];

    internal static Axis[] BuildYAxes(ChartValueBounds bounds) =>
    [
        new Axis
        {
            Name = "mmHg",
            NamePaint = new SolidColorPaint(MutedColour),
            MinLimit = bounds.Lowest,
            MaxLimit = bounds.Highest,
            LabelsPaint = new SolidColorPaint(MutedColour),
            SeparatorsPaint = new SolidColorPaint(GridColour) { StrokeThickness = 1 },
            TextSize = 11,
        },
    ];

    private static LineSeries<DateTimePoint> Line(
        string name,
        IReadOnlyList<TrendChartSample> samples,
        Func<TrendChartSample, double> value,
        LineAppearance appearance) =>
        new()
        {
            Name = name,
            Values = [.. samples.Select(point => DateTimePointFor(point.MeasuredAt, value(point)))],
            Stroke = new SolidColorPaint(appearance.Colour) { StrokeThickness = 2.5f },
            GeometryStroke = new SolidColorPaint(appearance.Colour) { StrokeThickness = 2.5f },
            GeometryFill = new SolidColorPaint(SurfaceColour),
            GeometrySize = appearance.PointSize,
            Fill = null,
            LineSmoothness = 0,
            XToolTipLabelFormatter = appearance.XTooltip is null ? null : point => appearance.XTooltip(point.Index),
            YToolTipLabelFormatter = appearance.YTooltip is null ? null : point => appearance.YTooltip(point.Index),
            IsHoverable = appearance.IsHoverable,
        };

    // Separate runs keep a crisis line from joining across a non-critical measurement.
    private static IEnumerable<LineSeries<DateTimePoint>> CrisisOverlays(
        string name,
        IReadOnlyList<TrendChartSample> samples,
        Func<TrendChartSample, double> value,
        int threshold)
    {
        var run = new List<DateTimePoint>();

        foreach (var sample in samples)
        {
            if (value(sample) >= threshold)
            {
                run.Add(DateTimePointFor(sample.MeasuredAt, value(sample)));
                continue;
            }

            if (run.Count > 0)
            {
                yield return CrisisOverlay(name, run);
                run = [];
            }
        }

        if (run.Count > 0)
        {
            yield return CrisisOverlay(name, run);
        }
    }

    private static LineSeries<DateTimePoint> CrisisOverlay(string name, IReadOnlyList<DateTimePoint> values) =>
        new()
        {
            Name = name,
            Values = values,
            Stroke = new SolidColorPaint(CrisisColour) { StrokeThickness = 3 },
            GeometryStroke = new SolidColorPaint(CrisisColour) { StrokeThickness = 3 },
            GeometryFill = new SolidColorPaint(CrisisColour),
            GeometrySize = 6,
            Fill = null,
            LineSmoothness = 0,
            IsVisibleAtLegend = false,
            IsHoverable = false,
        };

    private static LineSeries<TagPoint> TagMarkers(IReadOnlyList<TrendChartSample> samples) =>
        new()
        {
            Name = "Tag",
            Values = [.. samples
                .Where(point => !string.IsNullOrWhiteSpace(point.Tag))
                .Select(point => new TagPoint(point.MeasuredAt, point.Systolic, point.Tag!))],
            Mapping = (point, index) => new(point.MeasuredAt.LocalDateTime.Ticks, point.Value),
            Stroke = null,
            GeometryStroke = new SolidColorPaint(SurfaceColour) { StrokeThickness = 2 },
            GeometryFill = new SolidColorPaint(TaggedColour),
            GeometrySize = 16,
            Fill = null,
            LineSmoothness = 0,
            YToolTipLabelFormatter = point => point.Model?.Tag ?? string.Empty,
        };

    // Rings drawn around the systolic and diastolic points of whichever reading the user clicked
    // in the history table. Empty when nothing is selected, or when the selected reading falls
    // outside the chart's current period.
    private static IEnumerable<LineSeries<DateTimePoint>> SelectionMarkers(BloodPressureReading? highlighted)
    {
        if (highlighted is null)
        {
            yield break;
        }

        yield return SelectionMarker("Selected systolic", highlighted.MeasuredAt, highlighted.Systolic.MmHg);
        yield return SelectionMarker("Selected diastolic", highlighted.MeasuredAt, highlighted.Diastolic.MmHg);
    }

    private static LineSeries<DateTimePoint> SelectionMarker(string name, DateTimeOffset measuredAt, double value) =>
        new()
        {
            Name = name,
            Values = [DateTimePointFor(measuredAt, value)],
            Stroke = null,
            GeometryStroke = new SolidColorPaint(SelectionColour) { StrokeThickness = 3 },
            GeometryFill = new SolidColorPaint(SKColors.Transparent),
            GeometrySize = 22,
            Fill = null,
            IsVisibleAtLegend = false,
            IsHoverable = false,
        };

    private static DateTimePoint DateTimePointFor(DateTimeOffset measuredAt, double value) => new()
    {
        DateTime = measuredAt.LocalDateTime,
        Value = value,
    };

    private static TimeSpan LabelInterval(TrendPeriod period) => period switch
    {
        TrendPeriod.Week => TimeSpan.FromDays(1),
        TrendPeriod.Month => TimeSpan.FromDays(3),
        TrendPeriod.Quarter => TimeSpan.FromDays(7),
        TrendPeriod.Year => TimeSpan.FromDays(14),
        TrendPeriod.All => TimeSpan.FromDays(30),
        _ => TimeSpan.FromDays(7),
    };

    private sealed record LineAppearance(
        SKColor Colour,
        float PointSize,
        Func<int, string>? XTooltip,
        Func<int, string>? YTooltip,
        bool IsHoverable = true);

    private sealed record TagPoint(DateTimeOffset MeasuredAt, double Value, string Tag);
}
