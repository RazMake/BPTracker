using System.ComponentModel;
using System.Windows.Input;
using BPTracker.Domain.Readings;
using BPTracker.Presentation.Trends;
using LiveChartsCore.Defaults;
using LiveChartsCore.Drawing;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WPF;

namespace BPTracker.Desktop;

// Owns everything about the trend chart control: what it draws, where it is scrolled to and what
// the pointer is over. Keeping it out of MainWindow leaves the window as window chrome only.
internal sealed class TrendChartController
{
    private readonly CartesianChart _chart;
    private readonly TrendViewModel _trend;
    private readonly XamlDateTimeAxis _timeAxis = TrendChartBuilder.BuildTimeAxis();
    private readonly RectangularSection _pointerGuide = TrendChartBuilder.BuildPointerGuide();
    private readonly RectangularSection _selectionGuide = TrendChartBuilder.BuildSelectionGuide();

    private readonly LineSeries<DateTimePoint> _selectedSystolic =
        TrendChartBuilder.BuildSelectionMarker("Selected systolic");

    private readonly LineSeries<DateTimePoint> _selectedDiastolic =
        TrendChartBuilder.BuildSelectionMarker("Selected diastolic");

    private BloodPressureReading? _highlighted;

    internal TrendChartController(CartesianChart chart, TrendViewModel trend)
    {
        _chart = chart ?? throw new ArgumentNullException(nameof(chart));
        _trend = trend ?? throw new ArgumentNullException(nameof(trend));

        _trend.PropertyChanged += OnTrendChanged;
        _chart.MouseMove += OnMouseMove;
        _chart.MouseLeave += OnMouseLeave;
        _chart.MouseWheel += OnMouseWheel;
    }

    internal void Redraw()
    {
        var bounds = TrendChartBuilder.BuildBounds(_trend);

        ApplySelection();
        TrendChartBuilder.ApplyViewport(_timeAxis, _trend.Viewport);

        TrendChartBuilder.ApplyTheme(_chart);
        _chart.Series = TrendChartBuilder.BuildSeries(_trend, _selectedSystolic, _selectedDiastolic);
        _chart.Sections = TrendChartBuilder.BuildSections(bounds, _pointerGuide, _selectionGuide);
        _chart.XAxes = [_timeAxis];
        _chart.YAxes = TrendChartBuilder.BuildYAxes(bounds);
    }

    /// <summary>Rings one reading on the chart, or clears the ring when nothing is selected.</summary>
    internal void Highlight(BloodPressureReading? reading)
    {
        _highlighted = reading;
        ApplySelection();
    }

    public void Detach()
    {
        _trend.PropertyChanged -= OnTrendChanged;
        _chart.MouseMove -= OnMouseMove;
        _chart.MouseLeave -= OnMouseLeave;
        _chart.MouseWheel -= OnMouseWheel;
    }

    private void OnTrendChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TrendViewModel.Viewport))
        {
            TrendChartBuilder.ApplyViewport(_timeAxis, _trend.Viewport);
        }
        else if (e.PropertyName is nameof(TrendViewModel.IsLoading) or null)
        {
            Redraw();
        }
    }

    // Moves the guide and markers that are already on the chart. Reassigning Series, Sections or
    // Axes would make every series animate in again, which reads as a flicker.
    private void ApplySelection()
    {
        _selectionGuide.Xi = _highlighted?.MeasuredAt.LocalDateTime.Ticks;
        _selectionGuide.Xj = _selectionGuide.Xi;
        _selectionGuide.IsVisible = _highlighted is not null;

        TrendChartBuilder.MoveSelectionMarker(_selectedSystolic, _highlighted, reading => reading.Systolic.MmHg);
        TrendChartBuilder.MoveSelectionMarker(_selectedDiastolic, _highlighted, reading => reading.Diastolic.MmHg);
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        var pointer = e.GetPosition(_chart);
        var plot = _chart.CoreChart;
        var origin = plot.DrawMarginLocation;
        var size = plot.DrawMarginSize;

        if (pointer.X < origin.X || pointer.X > origin.X + size.Width ||
            pointer.Y < origin.Y || pointer.Y > origin.Y + size.Height)
        {
            _pointerGuide.IsVisible = false;
            return;
        }

        var data = _chart.ScalePixelsToData(new LvcPointD(pointer.X, pointer.Y), 0, 0);
        _pointerGuide.Xi = data.X;
        _pointerGuide.Xj = data.X;
        _pointerGuide.IsVisible = true;
    }

    private void OnMouseLeave(object sender, MouseEventArgs e) => _pointerGuide.IsVisible = false;

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        _trend.ScrollByNotches(-e.Delta / Mouse.MouseWheelDeltaForOneLine);
        e.Handled = true;
    }
}
