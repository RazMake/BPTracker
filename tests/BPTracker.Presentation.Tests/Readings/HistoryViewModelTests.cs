using BPTracker.Application.Abstractions;
using BPTracker.Application.Readings;
using BPTracker.Domain.Readings;
using BPTracker.Presentation.Readings;
using BPTracker.TestSupport;
using NSubstitute;

namespace BPTracker.Presentation.Tests.Readings;

public sealed class HistoryViewModelTests
{
    private readonly IReadingRepository _repository = Substitute.For<IReadingRepository>();
    private readonly TestClock _clock = new();

    private HistoryViewModel CreateViewModel() => new(
        new GetReadingHistoryUseCase(_repository, _clock),
        new RetractReadingUseCase(_repository, _clock));

    private void GivenReadings(params BloodPressureReading[] readings) =>
        _repository.GetRangeAsync(
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>()).Returns(readings);

    [Fact]
    public async Task RefreshPopulatesTheList()
    {
        GivenReadings([.. ReadingFactory.CreateDailySeries(3)]);
        var viewModel = CreateViewModel();

        await viewModel.RefreshAsync(CancellationToken.None);

        viewModel.Readings.Count.ShouldBe(3);
        viewModel.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public async Task RefreshQueriesTheConfiguredWindow()
    {
        var viewModel = CreateViewModel();
        viewModel.WindowDays = 14;

        await viewModel.RefreshAsync(CancellationToken.None);

        await _repository.Received().GetRangeAsync(
            _clock.LocalNow.AddDays(-14),
            _clock.LocalNow,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LatestExposesTheNewestReading()
    {
        var newest = ReadingFactory.Create(150, 95);
        GivenReadings(newest, ReadingFactory.Create(120, 80));
        var viewModel = CreateViewModel();

        await viewModel.RefreshAsync(CancellationToken.None);

        viewModel.Latest.ShouldBe(newest);
    }

    [Fact]
    public void LatestIsNullBeforeAnythingIsLoaded() =>
        CreateViewModel().Latest.ShouldBeNull();

    [Fact]
    public async Task RefreshRaisesChanged()
    {
        var viewModel = CreateViewModel();
        var raised = false;
        viewModel.Changed += (_, _) => raised = true;

        await viewModel.RefreshAsync(CancellationToken.None);

        raised.ShouldBeTrue();
    }

    [Fact]
    public async Task RetractRemovesTheReadingAndReloads()
    {
        var reading = ReadingFactory.Create();
        _repository.FindAsync(reading.Id, Arg.Any<CancellationToken>()).Returns(reading);
        GivenReadings(reading);

        var viewModel = CreateViewModel();
        await viewModel.RefreshAsync(CancellationToken.None);

        GivenReadings();
        await viewModel.RetractAsync(reading, CancellationToken.None);

        await _repository.Received().UpsertAsync(
            Arg.Is<BloodPressureReading>(candidate => candidate!.IsDeleted),
            Arg.Any<CancellationToken>());
        viewModel.Readings.ShouldBeEmpty();
    }

    [Fact]
    public async Task RetractIgnoresNull()
    {
        var viewModel = CreateViewModel();

        await viewModel.RetractAsync(null, CancellationToken.None);

        await _repository.DidNotReceive().UpsertAsync(
            Arg.Any<BloodPressureReading>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ConstructorRejectsNullDependencies()
    {
        var history = new GetReadingHistoryUseCase(_repository, _clock);
        var retract = new RetractReadingUseCase(_repository, _clock);

        Should.Throw<ArgumentNullException>(() => new HistoryViewModel(null!, retract));
        Should.Throw<ArgumentNullException>(() => new HistoryViewModel(history, null!));
    }
}
