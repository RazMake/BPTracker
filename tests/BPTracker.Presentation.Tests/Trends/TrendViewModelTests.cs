using System.Globalization;
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

    private void GivenHistoryReachesBackTo(DateTimeOffset earliest) =>
        _repository.GetEarliestMeasuredAtAsync(Arg.Any<CancellationToken>()).Returns(earliest);

    [Fact]
    public void DefaultsToTheMonthWindow() =>
        CreateViewModel().Period.ShouldBe(TrendPeriod.Month);

    [Fact]
    public void OffersNoWindowLongerThanAYear() =>
        TrendViewModel.AvailablePeriods.ShouldBe(
            [TrendPeriod.Week, TrendPeriod.Month, TrendPeriod.Quarter, TrendPeriod.Year]);

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
    public async Task ALongWindowIsScrollableAndOpensAtItsNewestEnd()
    {
        GivenReadings(6);
        var viewModel = CreateViewModel();
        viewModel.Period = TrendPeriod.Year;

        await viewModel.RefreshAsync(CancellationToken.None);

        viewModel.Viewport.VisibleDays.ShouldBe(TrendViewport.DefaultVisibleDays);
        viewModel.Viewport.CanScroll.ShouldBeTrue();
        viewModel.ScrollOffsetDays.ShouldBe(viewModel.Viewport.MaxOffsetDays);
        viewModel.Viewport.To.ShouldBe(_clock.LocalNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task AWindowThatFitsOnScreenDoesNotScroll()
    {
        GivenReadings(6);
        var viewModel = CreateViewModel();

        await viewModel.RefreshAsync(CancellationToken.None);

        viewModel.Viewport.CanScroll.ShouldBeFalse();
        viewModel.Viewport.MaxOffsetDays.ShouldBe(0, 0.001);
    }

    [Fact]
    public async Task ScrollingMovesTheVisibleSliceAndStopsAtTheEdges()
    {
        GivenReadings(6);
        var viewModel = CreateViewModel();
        viewModel.Period = TrendPeriod.Year;
        await viewModel.RefreshAsync(CancellationToken.None);

        viewModel.ScrollBy(-40);
        var moved = viewModel.Viewport.From;

        viewModel.ScrollBy(-10_000);

        moved.ShouldBe(_clock.LocalNow.AddDays(-70), TimeSpan.FromSeconds(1));
        viewModel.ScrollOffsetDays.ShouldBe(0, 0.001);
        viewModel.Viewport.From.ShouldBe(_clock.LocalNow.AddDays(-365), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task OneWheelNotchMovesATenthOfWhatIsOnScreen()
    {
        GivenReadings(6);
        var viewModel = CreateViewModel();
        viewModel.Period = TrendPeriod.Year;
        await viewModel.RefreshAsync(CancellationToken.None);
        var before = viewModel.ScrollOffsetDays;

        viewModel.ScrollByNotches(-2);

        (before - viewModel.ScrollOffsetDays).ShouldBe(TrendViewport.DefaultVisibleDays / 5d, 0.001);
    }

    [Fact]
    public async Task ThereIsNoOlderPageWhenNothingWasMeasuredBeforeTheWindow()
    {
        GivenReadings(6);
        var viewModel = CreateViewModel();

        await viewModel.RefreshAsync(CancellationToken.None);

        viewModel.CanPageOlder.ShouldBeFalse();
        viewModel.CanPageNewer.ShouldBeFalse();
        viewModel.PageIndex.ShouldBe(0);
    }

    [Fact]
    public async Task PagingBackLoadsTheWindowBeforeTheOneOnScreen()
    {
        GivenReadings(6);
        GivenHistoryReachesBackTo(_clock.LocalNow.AddDays(-400));
        var viewModel = CreateViewModel();
        await viewModel.RefreshAsync(CancellationToken.None);

        viewModel.CanPageOlder.ShouldBeTrue();
        await viewModel.PageOlderAsync(CancellationToken.None);

        viewModel.PageIndex.ShouldBe(1);
        viewModel.CanPageNewer.ShouldBeTrue();
        await _repository.Received().GetRangeAsync(
            _clock.LocalNow.AddDays(-60),
            _clock.LocalNow.AddDays(-30),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PagingForwardStopsAtTheMostRecentWindow()
    {
        GivenReadings(6);
        GivenHistoryReachesBackTo(_clock.LocalNow.AddDays(-400));
        var viewModel = CreateViewModel();
        await viewModel.RefreshAsync(CancellationToken.None);
        await viewModel.PageOlderAsync(CancellationToken.None);

        await viewModel.PageNewerAsync(CancellationToken.None);
        await viewModel.PageNewerAsync(CancellationToken.None);

        viewModel.PageIndex.ShouldBe(0);
        viewModel.CanPageNewer.ShouldBeFalse();
    }

    [Fact]
    public async Task ChangingThePeriodReturnsToTheMostRecentPage()
    {
        GivenReadings(6);
        GivenHistoryReachesBackTo(_clock.LocalNow.AddDays(-400));
        var viewModel = CreateViewModel();
        await viewModel.RefreshAsync(CancellationToken.None);
        await viewModel.PageOlderAsync(CancellationToken.None);

        viewModel.Period = TrendPeriod.Week;

        viewModel.PageIndex.ShouldBe(0);
    }

    [Fact]
    public async Task TheWindowLabelSpellsOutWhatIsLoaded()
    {
        GivenReadings(6);
        var viewModel = CreateViewModel();

        await viewModel.RefreshAsync(CancellationToken.None);

        viewModel.WindowLabel.ShouldContain(_clock.LocalNow.ToString("yyyy", CultureInfo.CurrentCulture));
        viewModel.WindowLabel.ShouldContain("-");
    }

    [Fact]
    public void ConstructorRejectsNullUseCase() =>
        Should.Throw<ArgumentNullException>(() => new TrendViewModel(null!));
}
