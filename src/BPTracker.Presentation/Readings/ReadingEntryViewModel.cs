using BPTracker.Application.Abstractions;
using BPTracker.Application.Readings;
using BPTracker.Domain.Readings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BPTracker.Presentation.Readings;

/// <summary>
/// Drives the fast two-number entry screen. Shared by the desktop and the phone.
/// </summary>
public sealed partial class ReadingEntryViewModel : ObservableObject
{
    /// <summary>Value the systolic field starts on when there is no previous reading.</summary>
    public const int DefaultSystolic = 120;

    /// <summary>Value the diastolic field starts on when there is no previous reading.</summary>
    public const int DefaultDiastolic = 80;

    private readonly AddReadingUseCase _addReading;
    private readonly IClock _clock;

    [ObservableProperty]
    public partial int Systolic { get; set; } = DefaultSystolic;

    [ObservableProperty]
    public partial int Diastolic { get; set; } = DefaultDiastolic;

    [ObservableProperty]
    public partial MeasurementArm Arm { get; set; } = MeasurementArm.Unspecified;

    [ObservableProperty]
    public partial BodyPosition Position { get; set; } = BodyPosition.Unspecified;

    [ObservableProperty]
    public partial string? Note { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsSaving { get; set; }

    /// <summary>Creates the view model.</summary>
    public ReadingEntryViewModel(AddReadingUseCase addReading, IClock clock)
    {
        _addReading = addReading ?? throw new ArgumentNullException(nameof(addReading));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>Live validation for the current pair.</summary>
    public ReadingEntryValidation Validation => ReadingEntryValidation.Validate(Systolic, Diastolic);

    /// <summary>Whether the current pair can be saved.</summary>
    public bool CanSave => Validation.IsValid && !IsSaving;

    /// <summary>Category preview for the current pair, or <see langword="null"/> when invalid.</summary>
    public BloodPressureCategory? PreviewCategory => Validation.Category;

    /// <summary>Raised after a reading is stored, so the host can navigate or show feedback.</summary>
    public event EventHandler<BloodPressureReading>? ReadingSaved;

    /// <summary>Nudges the systolic value, keeping it inside the plausible range.</summary>
    [RelayCommand]
    public void AdjustSystolic(int delta) =>
        Systolic = Math.Clamp(Systolic + delta, SystolicPressure.Minimum, SystolicPressure.Maximum);

    /// <summary>Nudges the diastolic value, keeping it inside the plausible range.</summary>
    [RelayCommand]
    public void AdjustDiastolic(int delta) =>
        Diastolic = Math.Clamp(Diastolic + delta, DiastolicPressure.Minimum, DiastolicPressure.Maximum);

    /// <summary>Validates and stores the current pair.</summary>
    [RelayCommand(CanExecute = nameof(CanSave))]
    public async Task SaveAsync(CancellationToken cancellationToken)
    {
        if (!CanSave)
        {
            return;
        }

        IsSaving = true;
        StatusMessage = null;

        try
        {
            var request = new AddReadingRequest(
                Systolic,
                Diastolic,
                _clock.LocalNow,
                new MeasurementContext { Arm = Arm, Position = Position, Note = Note });

            var saved = await _addReading.ExecuteAsync(request, cancellationToken).ConfigureAwait(true);

            Note = null;
            StatusMessage = "Saved";
            ReadingSaved?.Invoke(this, saved);
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>Seeds the fields from the previous reading, which is usually close to the next one.</summary>
    public void SeedFrom(BloodPressureReading? previous)
    {
        Systolic = previous?.Systolic.MmHg ?? DefaultSystolic;
        Diastolic = previous?.Diastolic.MmHg ?? DefaultDiastolic;
        Arm = previous?.Context.Arm ?? MeasurementArm.Unspecified;
        Position = previous?.Context.Position ?? BodyPosition.Unspecified;
        Note = null;
    }

    partial void OnSystolicChanged(int value) => OnPairChanged();

    partial void OnDiastolicChanged(int value) => OnPairChanged();

    partial void OnIsSavingChanged(bool value) => SaveCommand.NotifyCanExecuteChanged();

    private void OnPairChanged()
    {
        OnPropertyChanged(nameof(Validation));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(PreviewCategory));
        SaveCommand.NotifyCanExecuteChanged();
    }
}
