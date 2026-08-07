using BPTracker.Application.Abstractions;
using BPTracker.Application.Readings;
using BPTracker.Domain.Readings;
using BPTracker.Presentation.Readings;
using BPTracker.TestSupport;
using NSubstitute;

namespace BPTracker.Presentation.Tests.Readings;

public sealed class ReadingEntryViewModelTests
{
    private readonly IReadingRepository _repository = Substitute.For<IReadingRepository>();
    private readonly TestClock _clock = new();

    private ReadingEntryViewModel CreateViewModel() =>
        new(new AddReadingUseCase(_repository, _clock), _clock);

    [Fact]
    public void StartsOnTheMostCommonValues()
    {
        var viewModel = CreateViewModel();

        viewModel.Systolic.ShouldBe(ReadingEntryViewModel.DefaultSystolic);
        viewModel.Diastolic.ShouldBe(ReadingEntryViewModel.DefaultDiastolic);
        viewModel.CanSave.ShouldBeTrue();
    }

    [Fact]
    public void PreviewCategoryTracksTheCurrentPair()
    {
        var viewModel = CreateViewModel();

        viewModel.Systolic = 190;
        viewModel.PreviewCategory.ShouldBe(BloodPressureCategory.HypertensiveCrisis);
    }

    [Fact]
    public void InvalidPairBlocksSaving()
    {
        var viewModel = CreateViewModel();

        viewModel.Diastolic = 130;

        viewModel.CanSave.ShouldBeFalse();
        viewModel.PreviewCategory.ShouldBeNull();
        viewModel.SaveCommand.CanExecute(null).ShouldBeFalse();
    }

    [Fact]
    public void AdjustingClampsToThePlausibleRange()
    {
        var viewModel = CreateViewModel();

        viewModel.AdjustSystolic(1000);
        viewModel.Systolic.ShouldBe(SystolicPressure.Maximum);

        viewModel.AdjustSystolic(-1000);
        viewModel.Systolic.ShouldBe(SystolicPressure.Minimum);

        viewModel.AdjustDiastolic(1000);
        viewModel.Diastolic.ShouldBe(DiastolicPressure.Maximum);

        viewModel.AdjustDiastolic(-1000);
        viewModel.Diastolic.ShouldBe(DiastolicPressure.Minimum);
    }

    [Fact]
    public void AdjustingByASingleStepMovesOne()
    {
        var viewModel = CreateViewModel();

        viewModel.AdjustSystolic(1);
        viewModel.AdjustDiastolic(-1);

        viewModel.Systolic.ShouldBe(ReadingEntryViewModel.DefaultSystolic + 1);
        viewModel.Diastolic.ShouldBe(ReadingEntryViewModel.DefaultDiastolic - 1);
    }

    [Fact]
    public void ChangingAPressureRaisesChangeNotificationForDerivedState()
    {
        var viewModel = CreateViewModel();
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        viewModel.Systolic = 150;

        changed.ShouldContain(nameof(ReadingEntryViewModel.CanSave));
        changed.ShouldContain(nameof(ReadingEntryViewModel.PreviewCategory));
        changed.ShouldContain(nameof(ReadingEntryViewModel.Validation));
    }

    [Fact]
    public async Task SavingPersistsTheReadingAndReportsSuccess()
    {
        var viewModel = CreateViewModel();
        viewModel.Systolic = 135;
        viewModel.Diastolic = 88;
        viewModel.Arm = MeasurementArm.Left;

        BloodPressureReading? saved = null;
        viewModel.ReadingSaved += (_, reading) => saved = reading;

        await viewModel.SaveAsync(CancellationToken.None);

        await _repository.Received(1).UpsertAsync(
            Arg.Is<BloodPressureReading>(reading =>
                reading!.Systolic.MmHg == 135 &&
                reading.Diastolic.MmHg == 88 &&
                reading.Context.Arm == MeasurementArm.Left),
            Arg.Any<CancellationToken>());

        saved.ShouldNotBeNull();
        viewModel.StatusMessage.ShouldBe("Saved");
        viewModel.IsSaving.ShouldBeFalse();
    }

    [Fact]
    public async Task SavingClearsTheNoteSoItIsNotReusedByAccident()
    {
        var viewModel = CreateViewModel();
        viewModel.Note = "after a walk";

        await viewModel.SaveAsync(CancellationToken.None);

        viewModel.Note.ShouldBeNull();
    }

    [Fact]
    public async Task SavingAnInvalidPairDoesNothing()
    {
        var viewModel = CreateViewModel();
        viewModel.Diastolic = 130;

        await viewModel.SaveAsync(CancellationToken.None);

        await _repository.DidNotReceive().UpsertAsync(
            Arg.Any<BloodPressureReading>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveResetsTheBusyFlagWhenPersistenceFails()
    {
        _repository
            .UpsertAsync(Arg.Any<BloodPressureReading>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("disk full")));

        var viewModel = CreateViewModel();

        await Should.ThrowAsync<InvalidOperationException>(
            () => viewModel.SaveAsync(CancellationToken.None));

        viewModel.IsSaving.ShouldBeFalse();
        viewModel.CanSave.ShouldBeTrue();
    }

    [Fact]
    public void SeedFromCopiesThePreviousReading()
    {
        var viewModel = CreateViewModel();
        var previous = BloodPressureReading.Create(
            SystolicPressure.From(145),
            DiastolicPressure.From(95),
            TestClock.DefaultNow,
            TestClock.DefaultNow,
            new MeasurementContext { Arm = MeasurementArm.Right, Position = BodyPosition.Sitting });

        viewModel.SeedFrom(previous);

        viewModel.Systolic.ShouldBe(145);
        viewModel.Diastolic.ShouldBe(95);
        viewModel.Arm.ShouldBe(MeasurementArm.Right);
        viewModel.Position.ShouldBe(BodyPosition.Sitting);
        viewModel.Note.ShouldBeNull();
    }

    [Fact]
    public void SeedFromNullFallsBackToDefaults()
    {
        var viewModel = CreateViewModel();
        viewModel.Systolic = 170;

        viewModel.SeedFrom(null);

        viewModel.Systolic.ShouldBe(ReadingEntryViewModel.DefaultSystolic);
        viewModel.Diastolic.ShouldBe(ReadingEntryViewModel.DefaultDiastolic);
        viewModel.Arm.ShouldBe(MeasurementArm.Unspecified);
    }

    [Fact]
    public void ConstructorRejectsNullDependencies()
    {
        var useCase = new AddReadingUseCase(_repository, _clock);

        Should.Throw<ArgumentNullException>(() => new ReadingEntryViewModel(null!, _clock));
        Should.Throw<ArgumentNullException>(() => new ReadingEntryViewModel(useCase, null!));
    }
}
