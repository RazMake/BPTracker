using System.Globalization;
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

    private DragAdjustment _systolicDrag =
        DragAdjustment.For(DefaultSystolic, SystolicPressure.Minimum, SystolicPressure.Maximum);

    private DragAdjustment _diastolicDrag =
        DragAdjustment.For(DefaultDiastolic, DiastolicPressure.Minimum, DiastolicPressure.Maximum);

    [ObservableProperty]
    public partial int Systolic { get; set; } = DefaultSystolic;

    [ObservableProperty]
    public partial int Diastolic { get; set; } = DefaultDiastolic;

    [ObservableProperty]
    public partial MeasurementArm Arm { get; set; } = MeasurementArm.Unspecified;

    [ObservableProperty]
    public partial BodyPosition Position { get; set; } = BodyPosition.Unspecified;

    [ObservableProperty]
    public partial string? Tag { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsSaving { get; set; }

    /// <summary>
    /// Whether the pair on screen is the one just stored. Set after a save and cleared by any
    /// change, so the button cannot fire twice and record the same measurement.
    /// </summary>
    [ObservableProperty]
    public partial bool IsAlreadySaved { get; set; }

    /// <summary>Creates the view model.</summary>
    public ReadingEntryViewModel(AddReadingUseCase addReading, IClock clock)
    {
        _addReading = addReading ?? throw new ArgumentNullException(nameof(addReading));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>Live validation for the current pair.</summary>
    public ReadingEntryValidation Validation => ReadingEntryValidation.Validate(Systolic, Diastolic);

    /// <summary>Whether the current pair can be saved.</summary>
    public bool CanSave => Validation.IsValid && !IsSaving && !IsAlreadySaved;

    /// <summary>Category preview for the current pair, or <see langword="null"/> when invalid.</summary>
    public BloodPressureCategory? PreviewCategory => Validation.Category;

    /// <summary>The category preview in words, empty while the pair is invalid.</summary>
    public string PreviewCategoryName => BloodPressureCategoryName.For(PreviewCategory);

    /// <summary>Whether the systolic value on screen is a hypertensive crisis on its own.</summary>
    public bool IsSystolicCritical => Systolic >= CrisisThreshold.Systolic;

    /// <summary>Whether the diastolic value on screen is a hypertensive crisis on its own.</summary>
    public bool IsDiastolicCritical => Diastolic >= CrisisThreshold.Diastolic;

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

    /// <summary>Starts a slide gesture on the systolic value.</summary>
    public void BeginSystolicDrag() =>
        _systolicDrag = DragAdjustment.For(Systolic, SystolicPressure.Minimum, SystolicPressure.Maximum);

    /// <summary>Applies the total movement of the slide started by <see cref="BeginSystolicDrag"/>.</summary>
    public void DragSystolic(double totalX, double totalY) =>
        Systolic = _systolicDrag.ValueAt(totalX, totalY);

    /// <summary>Starts a slide gesture on the diastolic value.</summary>
    public void BeginDiastolicDrag() =>
        _diastolicDrag = DragAdjustment.For(Diastolic, DiastolicPressure.Minimum, DiastolicPressure.Maximum);

    /// <summary>Applies the total movement of the slide started by <see cref="BeginDiastolicDrag"/>.</summary>
    public void DragDiastolic(double totalX, double totalY) =>
        Diastolic = _diastolicDrag.ValueAt(totalX, totalY);

    /// <summary>Sets the systolic value from typed text. Returns false and changes nothing if it is not usable.</summary>
    public bool TrySetSystolic(string? text)
    {
        if (!TryParse(text, out var value) || !SystolicPressure.TryFrom(value, out _))
        {
            return false;
        }

        Systolic = value;
        return true;
    }

    /// <summary>Sets the diastolic value from typed text. Returns false and changes nothing if it is not usable.</summary>
    public bool TrySetDiastolic(string? text)
    {
        if (!TryParse(text, out var value) || !DiastolicPressure.TryFrom(value, out _))
        {
            return false;
        }

        Diastolic = value;
        return true;
    }

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
                new MeasurementContext { Arm = Arm, Position = Position, Tag = Tag });

            var saved = await _addReading.ExecuteAsync(request, cancellationToken).ConfigureAwait(true);

            Tag = null;
            StatusMessage = "Saved";
            IsAlreadySaved = true;
            ReadingSaved?.Invoke(this, saved);
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>Puts both numbers back to the values a typical entry starts from.</summary>
    [RelayCommand]
    public void Reset()
    {
        Systolic = DefaultSystolic;
        Diastolic = DefaultDiastolic;
        Tag = null;
        StatusMessage = null;

        // Explicit "start again", so the pair counts as unsaved even if it already held the defaults.
        IsAlreadySaved = false;
    }

    /// <summary>Seeds the fields from the previous reading, which is usually close to the next one.</summary>
    public void SeedFrom(BloodPressureReading? previous)
    {
        Systolic = previous?.Systolic.MmHg ?? DefaultSystolic;
        Diastolic = previous?.Diastolic.MmHg ?? DefaultDiastolic;
        Arm = previous?.Context.Arm ?? MeasurementArm.Unspecified;
        Position = previous?.Context.Position ?? BodyPosition.Unspecified;
        Tag = null;
    }

    partial void OnSystolicChanged(int value) => OnPairChanged();

    partial void OnDiastolicChanged(int value) => OnPairChanged();

    partial void OnIsSavingChanged(bool value) => SaveCommand.NotifyCanExecuteChanged();

    partial void OnIsAlreadySavedChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
    }

    private static bool TryParse(string? text, out int value) =>
        int.TryParse(text?.Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out value);

    private void OnPairChanged()
    {
        IsAlreadySaved = false;
        OnPropertyChanged(nameof(Validation));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(PreviewCategory));
        OnPropertyChanged(nameof(PreviewCategoryName));
        OnPropertyChanged(nameof(IsSystolicCritical));
        OnPropertyChanged(nameof(IsDiastolicCritical));
        SaveCommand.NotifyCanExecuteChanged();
    }
}
