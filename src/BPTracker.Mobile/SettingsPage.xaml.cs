using BPTracker.Presentation.Export;
using BPTracker.Presentation.Storage;

namespace BPTracker.Mobile;

public partial class SettingsPage : ContentPage
{
    private readonly StorageLocationViewModel _viewModel;
    private readonly ExportViewModel _export;

    public SettingsPage(StorageLocationViewModel viewModel, ExportViewModel export)
    {
        _viewModel = viewModel;
        _export = export;
        InitializeComponent();
        BindingContext = _viewModel;
    }

    private async void OnExportCsv(object? sender, EventArgs e) =>
        await RunExportAsync(_export.ExportCsvAsync);

    private async void OnExportChart(object? sender, EventArgs e) =>
        await RunExportAsync(_export.ExportChartAsync);

    private async void OnExportData(object? sender, EventArgs e) =>
        await RunExportAsync(_export.ExportDataAsync);

    private async Task RunExportAsync(Func<CancellationToken, Task> export)
    {
        ExportStatusLabel.Text = "Exporting...";
        try
        {
            await export(CancellationToken.None);
            ExportStatusLabel.Text = _export.StatusMessage;
        }
        catch (Exception exception)
        {
            ExportStatusLabel.Text = $"Export failed: {exception.Message}";
        }
    }

    private async void OnChangeFolder(object? sender, EventArgs e)
    {
        var entered = await DisplayPromptAsync(
            "Data folder",
            "Full path of the folder your sync app watches.",
            initialValue: _viewModel.DataFolder,
            keyboard: Keyboard.Text);

        if (string.IsNullOrWhiteSpace(entered))
        {
            return;
        }

        if (!_viewModel.ChangeFolder(entered))
        {
            await DisplayAlertAsync(
                "BPTracker",
                _viewModel.StatusMessage ?? "That folder cannot be used.",
                "OK");
        }
    }

    private async void OnCopyPath(object? sender, EventArgs e)
    {
        await Clipboard.Default.SetTextAsync(_viewModel.DataFolder);
        await DisplayAlertAsync("BPTracker", "Path copied.", "OK");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Only worth offering when the platform is actually withholding access.
        GrantAccessButton.IsVisible = !MobileStorage.HasAllFilesAccess();
    }

    private async void OnGrantAccess(object? sender, EventArgs e)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            return;
        }

        try
        {
            using var intent = new Android.Content.Intent(
                Android.Provider.Settings.ActionManageAppAllFilesAccessPermission,
                Android.Net.Uri.Parse("package:" + AppInfo.Current.PackageName));

            Platform.CurrentActivity?.StartActivity(intent);
        }
        catch (Android.Content.ActivityNotFoundException)
        {
            await DisplayAlertAsync(
                "BPTracker",
                "This device does not offer an all-files permission screen. Choose a folder the app can write to instead.",
                "OK");
        }
    }
}
