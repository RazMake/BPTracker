using System.ComponentModel;
using BPTracker.Application.Readings;
using BPTracker.Domain.Readings;
using BPTracker.Presentation.Readings;
using Microsoft.Extensions.DependencyInjection;

namespace BPTracker.Mobile;

public partial class EntryPage : ContentPage
{
    private readonly ReadingEntryViewModel _viewModel;
    private readonly GetReadingHistoryUseCase _history;
    private readonly IServiceProvider _services;

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
        var category = _viewModel.PreviewCategory;
        CategoryLabel.Text = category is null ? string.Empty : Describe(category.Value);
        CategoryLabel.TextColor = Color.FromArgb(ColourFor(category));
    }

    private void OnSystolicUp(object? sender, EventArgs e) => _viewModel.AdjustSystolic(1);

    private void OnSystolicDown(object? sender, EventArgs e) => _viewModel.AdjustSystolic(-1);

    private void OnDiastolicUp(object? sender, EventArgs e) => _viewModel.AdjustDiastolic(1);

    private void OnDiastolicDown(object? sender, EventArgs e) => _viewModel.AdjustDiastolic(-1);

    private void OnSystolicCompleted(object? sender, EventArgs e) => DiastolicEntry.Focus();

    private void OnToggleDetails(object? sender, EventArgs e)
    {
        DetailsPanel.IsVisible = !DetailsPanel.IsVisible;
        DetailsToggle.Text = DetailsPanel.IsVisible ? "Hide detail" : "Add detail";
    }

    private void OnArmChanged(object? sender, EventArgs e) =>
        _viewModel.Arm = ArmPicker.SelectedIndex switch
        {
            1 => MeasurementArm.Left,
            2 => MeasurementArm.Right,
            _ => MeasurementArm.Unspecified,
        };

    private void OnPositionChanged(object? sender, EventArgs e) =>
        _viewModel.Position = PositionPicker.SelectedIndex switch
        {
            1 => BodyPosition.Sitting,
            2 => BodyPosition.Standing,
            3 => BodyPosition.Lying,
            _ => BodyPosition.Unspecified,
        };

    private async void OnOpenSettings(object? sender, EventArgs e) =>
        await Navigation.PushAsync(_services.GetRequiredService<SettingsPage>());

    private static string Describe(BloodPressureCategory category) => category switch
    {
        BloodPressureCategory.Hypotension => "Low",
        BloodPressureCategory.Normal => "Normal",
        BloodPressureCategory.Elevated => "Elevated",
        BloodPressureCategory.HypertensionStage1 => "Stage 1",
        BloodPressureCategory.HypertensionStage2 => "Stage 2",
        BloodPressureCategory.HypertensiveCrisis => "Crisis - seek advice",
        _ => string.Empty,
    };

    private static string ColourFor(BloodPressureCategory? category) => category switch
    {
        BloodPressureCategory.Hypotension => "#3E8FB0",
        BloodPressureCategory.Normal => "#2E9E5B",
        BloodPressureCategory.Elevated => "#D9A420",
        BloodPressureCategory.HypertensionStage1 => "#E07A24",
        BloodPressureCategory.HypertensionStage2 => "#D64545",
        BloodPressureCategory.HypertensiveCrisis => "#8B1E1E",
        _ => "#6B7A88",
    };
}
