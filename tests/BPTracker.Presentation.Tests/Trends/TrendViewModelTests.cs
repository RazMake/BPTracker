using BPTracker.Application.Abstractions;
using BPTracker.Application.Trends;
using BPTracker.Presentation.Trends;
using BPTracker.TestSupport;
using NSubstitute;

namespace BPTracker.Presentation.Tests.Trends;

public sealed class TrendViewModelTests
{
    private readonly IReadingRepository _repository = Substitute.For<IReadingRepository>();
    private readonly TestClock _clock = new();

    private TrendViewModel CreateViewModel() => new(new GetTrendUseCase(_repository, _clock));

    private void GivenReadings(int days) =>
        _repository.GetRangeAsync(
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>()).Returns(ReadingFactory.CreateDailySeries(days));

    [Fact]
    public void DefaultsToTheMonthWindow() =>
        CreateViewModel().Period.ShouldBe(TrendPeriod.Month);

    [Fact]
    public void OffersEveryWindow() =>
        TrendViewModel.AvailablePeriods.ShouldContain(TrendPeriod.All);

    [Fact]
    public async Task RefreshPopulatesBothSeries()
    {
        GivenReadings(6);
        var viewModel = CreateViewModel();

        await viewModel.RefreshAsync(CancellationToken.None);

        viewModel.Readings.Count.ShouldBe(6);
        viewModel.ChartSamples.Count.ShouldBe(6);
        viewModel.Daily.Count.ShouldBe(6);
        viewModel.Smoothed.Count.ShouldBe(6);
        viewModel.HasData.ShouldBeTrue();
        viewModel.Summary.ReadingCount.ShouldBe(6);
        viewModel.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public async Task RefreshReplacesRatherThanAppends()
    {
        GivenReadings(4);
        var viewModel = CreateViewModel();

        await viewModel.RefreshAsync(CancellationToken.None);
        await viewModel.RefreshAsync(CancellationToken.None);

        viewModel.Daily.Count.ShouldBe(4);
    }

    [Fact]
    public async Task RefreshWithNoReadingsReportsNoData()
    {
        var viewModel = CreateViewModel();

        await viewModel.RefreshAsync(CancellationToken.None);

        viewModel.HasData.ShouldBeFalse();
        viewModel.Readings.ShouldBeEmpty();
        viewModel.ChartSamples.ShouldBeEmpty();
        viewModel.Daily.ShouldBeEmpty();
    }

    [Fact]
    public void ChangingThePeriodTriggersAReload()
    {
        GivenReadings(3);
        var viewModel = CreateViewModel();

        viewModel.Period = TrendPeriod.Week;

        _repository.Received().GetRangeAsync(
            _clock.LocalNow.AddDays(-7),
            _clock.LocalNow,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ConstructorRejectsNullUseCase() =>
        Should.Throw<ArgumentNullException>(() => new TrendViewModel(null!));
}
