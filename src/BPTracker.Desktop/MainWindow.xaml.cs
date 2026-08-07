using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using BPTracker.Presentation;

namespace BPTracker.Desktop;

public partial class MainWindow : Window
{
    private readonly DashboardViewModel _dashboard;

    public MainWindow(DashboardViewModel dashboard)
    {
        _dashboard = dashboard ?? throw new ArgumentNullException(nameof(dashboard));

        InitializeComponent();

        DataContext = _dashboard;
        _dashboard.Trend.PropertyChanged += OnTrendChanged;
        _dashboard.RefreshFailed += OnRefreshFailed;

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
        TrendChartBuilder.ApplyTheme(TrendChart);
        TrendChart.Series = TrendChartBuilder.BuildSeries(_dashboard.Trend);
        TrendChart.Sections = TrendChartBuilder.BuildSections();
        TrendChart.XAxes = TrendChartBuilder.BuildXAxes(_dashboard.Trend);
        TrendChart.YAxes = TrendChartBuilder.BuildYAxes();
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
