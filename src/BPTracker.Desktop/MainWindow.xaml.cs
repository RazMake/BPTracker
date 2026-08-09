using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using BPTracker.Domain.Readings;
using BPTracker.Presentation;

namespace BPTracker.Desktop;

public partial class MainWindow : Window
{
    private readonly DashboardViewModel _dashboard;
    private readonly TrendChartController _trendChart;

    public MainWindow(DashboardViewModel dashboard, DesktopExportRenderer exportRenderer)
    {
        _dashboard = dashboard ?? throw new ArgumentNullException(nameof(dashboard));
        ArgumentNullException.ThrowIfNull(exportRenderer);

        InitializeComponent();

        exportRenderer.Chart = TrendChart;
        DataContext = _dashboard;
        _trendChart = new TrendChartController(TrendChart, _dashboard.Trend);
        _dashboard.RefreshFailed += OnRefreshFailed;
        HistoryGrid.SelectionChanged += OnHistorySelectionChanged;
        HistoryGrid.IsKeyboardFocusWithinChanged += OnHistoryFocusChanged;

        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await _dashboard.InitializeAsync(CancellationToken.None);
            _trendChart.Redraw();
        }
        catch (Exception exception)
        {
            ShowError("Could not load your readings.", exception);
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _dashboard.RefreshFailed -= OnRefreshFailed;
        HistoryGrid.SelectionChanged -= OnHistorySelectionChanged;
        HistoryGrid.IsKeyboardFocusWithinChanged -= OnHistoryFocusChanged;
        _trendChart.Detach();
        _dashboard.Dispose();
    }

    private static void OnRefreshFailed(object? sender, Exception exception) =>
        ShowError("Saved, but the chart could not be refreshed.", exception);

    private void OnHistorySelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) =>
        _trendChart.Highlight(HistoryGrid.SelectedItem as BloodPressureReading);

    // IsKeyboardFocusWithin rather than LostKeyboardFocus, which also fires when focus moves from
    // one cell of the grid to another.
    private void OnHistoryFocusChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (!(bool)e.NewValue)
        {
            HistoryGrid.UnselectAll();
        }
    }

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
