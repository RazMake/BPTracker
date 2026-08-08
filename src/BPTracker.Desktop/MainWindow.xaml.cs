using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using BPTracker.Domain.Readings;
using BPTracker.Presentation;
using LiveChartsCore.Drawing;
using LiveChartsCore.SkiaSharpView;

namespace BPTracker.Desktop;

public partial class MainWindow : Window
{
    private readonly DashboardViewModel _dashboard;
    private readonly RectangularSection _pointerGuide = TrendChartBuilder.BuildPointerGuide();
    private readonly RectangularSection _selectionGuide = TrendChartBuilder.BuildSelectionGuide();
    private BloodPressureReading? _selectedReading;

    public MainWindow(DashboardViewModel dashboard)
    {
        _dashboard = dashboard ?? throw new ArgumentNullException(nameof(dashboard));

        InitializeComponent();

        DataContext = _dashboard;
        _dashboard.Trend.PropertyChanged += OnTrendChanged;
        _dashboard.RefreshFailed += OnRefreshFailed;
        TrendChart.MouseMove += OnTrendChartMouseMove;
        TrendChart.MouseLeave += OnTrendChartMouseLeave;
        HistoryGrid.SelectionChanged += OnHistorySelectionChanged;

        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await _dashboard.InitializeAsync(CancellationToken.None);
            RedrawChart();
        }
        catch (Exception exception)
        {
            ShowError("Could not load your readings.", exception);
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _dashboard.Trend.PropertyChanged -= OnTrendChanged;
        _dashboard.RefreshFailed -= OnRefreshFailed;
        TrendChart.MouseMove -= OnTrendChartMouseMove;
        TrendChart.MouseLeave -= OnTrendChartMouseLeave;
        HistoryGrid.SelectionChanged -= OnHistorySelectionChanged;
        _dashboard.Dispose();
    }

    private void OnTrendChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DashboardViewModel.Trend.IsLoading) or null)
        {
            RedrawChart();
        }
    }

    private static void OnRefreshFailed(object? sender, Exception exception) =>
        ShowError("Saved, but the chart could not be refreshed.", exception);

    private void RedrawChart()
    {
        var bounds = TrendChartBuilder.BuildBounds(_dashboard.Trend);

        _selectionGuide.Xi = _selectedReading?.MeasuredAt.LocalDateTime.Ticks;
        _selectionGuide.Xj = _selectionGuide.Xi;
        _selectionGuide.IsVisible = _selectedReading is not null;

        TrendChartBuilder.ApplyTheme(TrendChart);
        TrendChart.Series = TrendChartBuilder.BuildSeries(_dashboard.Trend, _selectedReading);
        TrendChart.Sections = TrendChartBuilder.BuildSections(bounds, _pointerGuide, _selectionGuide);
        TrendChart.XAxes = TrendChartBuilder.BuildXAxes(_dashboard.Trend);
        TrendChart.YAxes = TrendChartBuilder.BuildYAxes(bounds);
    }

    private void OnHistorySelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        _selectedReading = HistoryGrid.SelectedItem as BloodPressureReading;
        RedrawChart();
    }

    private void OnTrendChartMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        var pointer = e.GetPosition(TrendChart);
        var plot = TrendChart.CoreChart;
        var origin = plot.DrawMarginLocation;
        var size = plot.DrawMarginSize;

        if (pointer.X < origin.X || pointer.X > origin.X + size.Width ||
            pointer.Y < origin.Y || pointer.Y > origin.Y + size.Height)
        {
            _pointerGuide.IsVisible = false;
            return;
        }

        var data = TrendChart.ScalePixelsToData(new LvcPointD(pointer.X, pointer.Y), 0, 0);
        _pointerGuide.Xi = data.X;
        _pointerGuide.Xj = data.X;
        _pointerGuide.IsVisible = true;
    }

    private void OnTrendChartMouseLeave(object sender, System.Windows.Input.MouseEventArgs e) =>
        _pointerGuide.IsVisible = false;

    private async void OnCheckForUpdates(object sender, RoutedEventArgs e)
    {
        UpdateStatusText.Text = "Checking for updates...";
        try
        {
            var version = await UpdateService.CheckAndApplyAsync();
            UpdateStatusText.Text = version is null
                ? "You are on the latest version."
                : $"Updating to {version}...";
        }
        catch (Exception exception)
        {
            UpdateStatusText.Text = $"Update check failed: {exception.Message}";
        }
    }

    private void OnChangeDataFolder(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Choose the folder your sync tool watches",
            InitialDirectory = _dashboard.Storage.DataFolder,
        };

        if (dialog.ShowDialog(this) == true)
        {
            _dashboard.Storage.ChangeFolder(dialog.FolderName);
        }
    }

    private void OnOpenDataFolder(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(_dashboard.Storage.DataFolder) { UseShellExecute = true });
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            ShowError("Could not open the folder.", exception);
        }
    }

    private static void ShowError(string message, Exception exception) =>
        MessageBox.Show(
            $"{message}\n\n{exception.Message}",
            "BPTracker",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
}
