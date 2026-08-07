using BPTracker.Application.Abstractions;
using BPTracker.Application.Readings;
using BPTracker.Presentation.Charts;
using BPTracker.TestSupport;
using NSubstitute;

namespace BPTracker.Presentation.Tests.Charts;

public sealed class ChartViewModelTests
{
    private const double Tolerance = 1e-9;

    private readonly IReadingRepository _repository = Substitute.For<IReadingRepository>();
    private readonly TestClock _clock = new();

    private ChartViewModel CreateViewModel() => new(new GetReadingHistoryUseCase(_repository, _clock));

    private void GivenReadings(int days) =>
        _repository.GetRangeAsync(
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>()).Returns(ReadingFactory.CreateDailySeries(days));

    private async Task<ChartViewModel> LoadedViewModel(int days, double width = 100, double height = 200)
    {
        GivenReadings(days);
        var viewModel = CreateViewModel();
        await viewModel.RefreshAsync(CancellationToken.None);
        viewModel.Resize(width, height);
        return viewModel;
    }

    [Fact]
    public void ConstructorRejectsANullUseCase() =>
        Should.Throw<ArgumentNullException>(() => new ChartViewModel(null!));

    [Fact]
    public async Task RefreshLoadsEveryMeasurementOldestFirst()
    {
        GivenReadings(3);
        var viewModel = CreateViewModel();

        await viewModel.RefreshAsync(CancellationToken.None);

        viewModel.HasData.ShouldBeTrue();
        viewModel.Samples.Count.ShouldBe(3);
        viewModel.Samples[0].MeasuredAt.ShouldBeLessThan(viewModel.Samples[2].MeasuredAt);
        viewModel.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public async Task RefreshAsksForTheWholeHistory()
    {
        GivenReadings(1);
        var viewModel = CreateViewModel();

        await viewModel.RefreshAsync(CancellationToken.None);

        await _repository.Received().GetRangeAsync(
            _clock.LocalNow.AddDays(-ChartViewModel.WindowDays),
            _clock.LocalNow,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WithNoMeasurementsThereIsNothingToDraw()
    {
        var viewModel = CreateViewModel();

        await viewModel.RefreshAsync(CancellationToken.None);

        viewModel.HasData.ShouldBeFalse();
        viewModel.BuildFrame().HasData.ShouldBeFalse();
    }

    [Fact]
    public async Task ResizingRemembersTheRoomTheChartHas()
    {
        var viewModel = await LoadedViewModel(5, width: 120, height: 240);

        viewModel.PlotWidth.ShouldBe(120, Tolerance);
        viewModel.PlotHeight.ShouldBe(240, Tolerance);
    }

    [Fact]
    public async Task ANegativeSizeIsTreatedAsNoRoom()
    {
        var viewModel = await LoadedViewModel(5, width: -10, height: -10);

        viewModel.PlotWidth.ShouldBe(0, Tolerance);
        viewModel.PlotHeight.ShouldBe(0, Tolerance);
    }

    [Fact]
    public async Task TheNewestMeasurementIsShownFirst()
    {
        var viewModel = await LoadedViewModel(5);

        viewModel.Offset.ShouldBe(viewModel.ContentWidth - viewModel.PlotWidth, Tolerance);
    }

    [Fact]
    public async Task DraggingRightGoesBackInTime()
    {
        var viewModel = await LoadedViewModel(5);
        var start = viewModel.Offset;

        viewModel.BeginScroll();
        viewModel.Scroll(50);

        viewModel.Offset.ShouldBe(start - 50, Tolerance);
    }

    [Fact]
    public async Task ScrollingIsMeasuredFromWhereTheGestureStarted()
    {
        var viewModel = await LoadedViewModel(5);
        var start = viewModel.Offset;

        viewModel.BeginScroll();
        viewModel.Scroll(20);
        viewModel.Scroll(50);

        viewModel.Offset.ShouldBe(start - 50, Tolerance);
    }

    [Fact]
    public async Task ScrollingCannotRunOffEitherEnd()
    {
        var viewModel = await LoadedViewModel(5);

        viewModel.BeginScroll();
        viewModel.Scroll(100_000);
        viewModel.Offset.ShouldBe(0, Tolerance);

        viewModel.BeginScroll();
        viewModel.Scroll(-100_000);
        viewModel.Offset.ShouldBe(viewModel.ContentWidth - viewModel.PlotWidth, Tolerance);
    }

    [Fact]
    public async Task ScrollToNewestReturnsToTheEnd()
    {
        var viewModel = await LoadedViewModel(5);
        viewModel.BeginScroll();
        viewModel.Scroll(100_000);

        viewModel.ScrollToNewest();

        viewModel.Offset.ShouldBe(viewModel.ContentWidth - viewModel.PlotWidth, Tolerance);
    }

    [Fact]
    public async Task AHistoryThatFitsOnScreenCannotBeScrolled()
    {
        var viewModel = await LoadedViewModel(1, width: 500);

        viewModel.CanScroll.ShouldBeFalse();
        viewModel.Offset.ShouldBe(0, Tolerance);
    }

    [Fact]
    public async Task AHistoryWiderThanTheScreenCanBeScrolled()
    {
        var viewModel = await LoadedViewModel(30, width: 100);

        viewModel.CanScroll.ShouldBeTrue();
    }

    [Fact]
    public async Task HoldingAndReleasingShowsThenHidesTheReadOut()
    {
        var viewModel = await LoadedViewModel(5);

        viewModel.Inspect(40);
        viewModel.CursorX.ShouldNotBeNull();
        viewModel.BuildFrame().Cursor.ShouldNotBeNull();

        viewModel.ClearCursor();
        viewModel.CursorX.ShouldBeNull();
        viewModel.BuildFrame().Cursor.ShouldBeNull();
    }

    [Fact]
    public async Task EveryChangeThatAltersTheDrawingAsksForARedraw()
    {
        var viewModel = await LoadedViewModel(5);
        var redraws = 0;
        viewModel.FrameChanged += (_, _) => redraws++;

        viewModel.Resize(120, 240);
        viewModel.Inspect(10);
        viewModel.ClearCursor();
        viewModel.BeginScroll();
        viewModel.Scroll(10);
        viewModel.ScrollToNewest();

        redraws.ShouldBe(5);
    }

    [Fact]
    public async Task TouchesNearTheBottomInspectAndTouchesNearTheTopScroll()
    {
        var viewModel = await LoadedViewModel(5, height: 200);

        viewModel.GestureFor(10).ShouldBe(ChartGesture.Scroll);
        viewModel.GestureFor(190).ShouldBe(ChartGesture.Inspect);
    }

    [Fact]
    public async Task TheFrameCarriesOnePointPerMeasurement()
    {
        var viewModel = await LoadedViewModel(4);

        viewModel.BuildFrame().Systolic.Dots.Count.ShouldBe(4);
    }
}
