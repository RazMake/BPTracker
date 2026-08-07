using BPTracker.Application.Abstractions;
using BPTracker.Application.Readings;
using BPTracker.Domain.Readings;
using BPTracker.Presentation.Readings;
using BPTracker.TestSupport;
using NSubstitute;

namespace BPTracker.Presentation.Tests.Readings;

public sealed class ReadingEntrySavingTests
{
    private readonly IReadingRepository _repository = Substitute.For<IReadingRepository>();
    private readonly TestClock _clock = new();

    private ReadingEntryViewModel CreateViewModel() =>
        new(new AddReadingUseCase(_repository, _clock), _clock);

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
        viewModel.IsSaving.ShouldBeFalse();
    }

    [Fact]
    public async Task TheSamePairCannotBeSavedTwiceInARow()
    {
        var viewModel = CreateViewModel();

        await viewModel.SaveAsync(CancellationToken.None);

        viewModel.IsAlreadySaved.ShouldBeTrue();
        viewModel.CanSave.ShouldBeFalse();
        viewModel.SaveCommand.CanExecute(null).ShouldBeFalse();
    }

    [Fact]
    public async Task ChangingEitherNumberMakesItSaveableAgain()
    {
        var viewModel = CreateViewModel();
        await viewModel.SaveAsync(CancellationToken.None);

        viewModel.Systolic += 1;

        viewModel.IsAlreadySaved.ShouldBeFalse();
        viewModel.CanSave.ShouldBeTrue();
    }

    [Fact]
    public async Task ASecondSaveOfAnUnchangedPairStoresNothing()
    {
        var viewModel = CreateViewModel();
        await viewModel.SaveAsync(CancellationToken.None);

        await viewModel.SaveAsync(CancellationToken.None);

        await _repository.Received(1).UpsertAsync(
            Arg.Any<BloodPressureReading>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ResetReturnsBothNumbersToTheirDefaults()
    {
        var viewModel = CreateViewModel();
        viewModel.Systolic = 165;
        viewModel.Diastolic = 99;
        viewModel.Tag = "after a walk";

        viewModel.Reset();

        viewModel.Systolic.ShouldBe(ReadingEntryViewModel.DefaultSystolic);
        viewModel.Diastolic.ShouldBe(ReadingEntryViewModel.DefaultDiastolic);
        viewModel.Tag.ShouldBeNull();
        viewModel.StatusMessage.ShouldBeNull();
    }

    [Fact]
    public async Task ResetMakesAJustSavedDefaultPairSaveableAgain()
    {
        var viewModel = CreateViewModel();
        await viewModel.SaveAsync(CancellationToken.None);

        viewModel.ResetCommand.Execute(null);

        viewModel.CanSave.ShouldBeTrue();
    }

    [Fact]
    public async Task SavingClearsTheTagSoItIsNotReusedByAccident()
    {
        var viewModel = CreateViewModel();
        viewModel.Tag = "after a walk";

        await viewModel.SaveAsync(CancellationToken.None);

        viewModel.Tag.ShouldBeNull();
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
}
