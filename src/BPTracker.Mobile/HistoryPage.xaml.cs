using BPTracker.Presentation.Readings;

namespace BPTracker.Mobile;

public partial class HistoryPage : ContentPage
{
    private readonly HistoryViewModel _viewModel;

    public HistoryPage(HistoryViewModel viewModel)
    {
        _viewModel = viewModel;

        InitializeComponent();
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefreshAsync();
    }

    private async void OnRefresh(object? sender, EventArgs e) => await RefreshAsync();

    private async Task RefreshAsync()
    {
        try
        {
            await _viewModel.RefreshAsync(CancellationToken.None);
        }
        catch (Exception exception)
        {
            await DisplayAlertAsync("BPTracker", $"Could not read your history.\n\n{exception.Message}", "OK");
        }
    }
}
