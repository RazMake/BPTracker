using System.ComponentModel;
using System.Globalization;
using BPTracker.Application.Readings;
using BPTracker.Domain.Readings;
using BPTracker.Presentation.Readings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Layouts;

namespace BPTracker.Mobile;

public partial class EntryPage : ContentPage
{
    private const double PortraitValueFontSize = 76;
    private const double LandscapeValueFontSize = 52;

    private const string CrisisColour = "#FF2D3E";
    private const string NormalValueColour = "#E9EEF3";

    private readonly ReadingEntryViewModel _viewModel;
    private readonly GetReadingHistoryUseCase _history;
    private readonly IServiceProvider _services;

    private bool? _isLandscape;

    public EntryPage(
        ReadingEntryViewModel viewModel,
        GetReadingHistoryUseCase history,
        IServiceProvider services)
    {
        _viewModel = viewModel;
        _history = history;
        _services = services;

        InitializeComponent();

        BindingContext = _viewModel;
        _viewModel.PropertyChanged += OnViewModelChanged;
        _viewModel.ReadingSaved += OnReadingSaved;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            var recent = await _history.ExecuteAsync(30);
            _viewModel.SeedFrom(recent.Count > 0 ? recent[0] : null);
        }
        catch (Exception exception)
        {
            await DisplayAlertAsync("BPTracker", $"Could not read your history.\n\n{exception.Message}", "OK");
        }

        UpdateCategory();
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ReadingEntryViewModel.PreviewCategory) or null)
        {
            UpdateCategory();
        }
    }

    // Confirm physically so the user can look away the moment they tap.
    private static void OnReadingSaved(object? sender, BloodPressureReading reading)
    {
        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        catch (Exception exception) when (
            exception is FeatureNotSupportedException or PermissionException or NotSupportedException)
        {
            // The reading is already saved. A missing vibrator or permission must never
            // turn a successful save into a crash.
        }
    }

    private void UpdateCategory()
    {
        CategoryLabel.Text = _viewModel.PreviewCategoryName;
        CategoryLabel.TextColor = Color.FromArgb(ColourFor(_viewModel.PreviewCategory));

        SystolicValue.TextColor = Color.FromArgb(
            _viewModel.IsSystolicCritical ? CrisisColour : NormalValueColour);

        DiastolicValue.TextColor = Color.FromArgb(
            _viewModel.IsDiastolicCritical ? CrisisColour : NormalValueColour);
    }

    private void OnPageSizeChanged(object? sender, EventArgs e) => ApplyOrientation(Width > Height);

    // Three panels, laid out down the screen in portrait and across it in landscape.
    private void ApplyOrientation(bool landscape)
    {
        if (_isLandscape == landscape)
        {
            return;
        }

        _isLandscape = landscape;

        RootLayout.Direction = landscape ? FlexDirection.Row : FlexDirection.Column;
        SystolicValue.FontSize = landscape ? LandscapeValueFontSize : PortraitValueFontSize;
        DiastolicValue.FontSize = SystolicValue.FontSize;
    }

    private void OnSystolicPan(object? sender, PanUpdatedEventArgs e)
    {
        if (e.StatusType == GestureStatus.Started)
        {
            _viewModel.BeginSystolicDrag();
        }
        else if (e.StatusType == GestureStatus.Running)
        {
            _viewModel.DragSystolic(e.TotalX, e.TotalY);
        }
    }

    private void OnDiastolicPan(object? sender, PanUpdatedEventArgs e)
    {
        if (e.StatusType == GestureStatus.Started)
        {
            _viewModel.BeginDiastolicDrag();
        }
        else if (e.StatusType == GestureStatus.Running)
        {
            _viewModel.DragDiastolic(e.TotalX, e.TotalY);
        }
    }

    private async void OnSystolicTapped(object? sender, TappedEventArgs e) =>
        _ = _viewModel.TrySetSystolic(await AskForNumber("Systolic", _viewModel.Systolic));

    private async void OnDiastolicTapped(object? sender, TappedEventArgs e) =>
        _ = _viewModel.TrySetDiastolic(await AskForNumber("Diastolic", _viewModel.Diastolic));

    private Task<string> AskForNumber(string title, int current) => DisplayPromptAsync(
        title,
        "Pressure in mmHg",
        initialValue: current.ToString(CultureInfo.CurrentCulture),
        maxLength: 3,
        keyboard: Keyboard.Numeric);

    private async void OnOpenChart(object? sender, EventArgs e) =>
        await Navigation.PushAsync(_services.GetRequiredService<ChartPage>());

    private async void OnOpenSettings(object? sender, EventArgs e) =>
        await Navigation.PushAsync(_services.GetRequiredService<SettingsPage>());

    private static string ColourFor(BloodPressureCategory? category) => category switch
    {
        BloodPressureCategory.Hypotension => "#4C8DFF",
        BloodPressureCategory.Normal => "#3FBF77",
        BloodPressureCategory.Elevated => "#D9A420",
        BloodPressureCategory.HypertensionStage1 => "#E8912D",
        BloodPressureCategory.HypertensionStage2 => "#E5484D",
        BloodPressureCategory.HypertensiveCrisis => "#FF6B6B",
        _ => "#8A98A6",
    };
}
