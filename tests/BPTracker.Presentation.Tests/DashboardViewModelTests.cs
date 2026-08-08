using BPTracker.Application.Abstractions;
using BPTracker.Application.Readings;
using BPTracker.Application.Trends;
using BPTracker.Presentation;
using BPTracker.Presentation.Export;
using BPTracker.Presentation.Readings;
using BPTracker.Presentation.Storage;
using BPTracker.Presentation.Trends;
using BPTracker.TestSupport;
using NSubstitute;

namespace BPTracker.Presentation.Tests;

public sealed class DashboardViewModelTests
{
    private readonly IReadingRepository _repository = Substitute.For<IReadingRepository>();
    private readonly TestClock _clock = new();
    private readonly TestStorageLocation _location = new();

    private DashboardViewModel CreateViewModel() => new(
        new ReadingEntryViewModel(new AddReadingUseCase(_repository, _clock), _clock),
        new HistoryViewModel(
            new GetReadingHistoryUseCase(_repository, _clock),
            new RetractReadingUseCase(_repository, _clock)),
        new TrendViewModel(new GetTrendUseCase(_repository, _clock)),
        new StorageLocationViewModel(_location),
        CreateExport());

    private ExportViewModel CreateExport() => new(
        new GetReadingHistoryUseCase(_repository, _clock),
        Substitute.For<IExportRenderer>(),
        _clock);

    private void GivenReadings(params Domain.Readings.BloodPressureReading[] readings) =>
        _repository.GetRangeAsync(
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>()).Returns(readings);

    [Fact]
    public async Task InitializeLoadsHistoryAndTrend()
    {
        GivenReadings([.. ReadingFactory.CreateDailySeries(4)]);
        using var viewModel = CreateViewModel();

        await viewModel.InitializeAsync(CancellationToken.None);

        viewModel.History.Readings.Count.ShouldBe(4);
        viewModel.Trend.Daily.Count.ShouldBe(4);
    }

    [Fact]
    public async Task InitializeSeedsEntryFromTheLatestReading()
    {
        GivenReadings(ReadingFactory.Create(145, 95));
        using var viewModel = CreateViewModel();

        await viewModel.InitializeAsync(CancellationToken.None);

        viewModel.Entry.Systolic.ShouldBe(145);
        viewModel.Entry.Diastolic.ShouldBe(95);
    }

    [Fact]
    public async Task InitializeFallsBackToDefaultsWithNoHistory()
    {
        using var viewModel = CreateViewModel();

        await viewModel.InitializeAsync(CancellationToken.None);

        viewModel.Entry.Systolic.ShouldBe(ReadingEntryViewModel.DefaultSystolic);
    }

    [Fact]
    public async Task SavingARefreshesTheListAndTheChart()
    {
        using var viewModel = CreateViewModel();
        await viewModel.InitializeAsync(CancellationToken.None);

        GivenReadings([.. ReadingFactory.CreateDailySeries(2)]);
        await viewModel.Entry.SaveAsync(CancellationToken.None);

        viewModel.History.Readings.Count.ShouldBe(2);
        viewModel.Trend.Daily.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ChangingTheDataFolderReloadsEverything()
    {
        using var viewModel = CreateViewModel();
        await viewModel.InitializeAsync(CancellationToken.None);

        GivenReadings([.. ReadingFactory.CreateDailySeries(3)]);
        viewModel.Storage.ChangeFolder(@"D:\Sync\BP");

        viewModel.History.Readings.Count.ShouldBe(3);
    }

    [Fact]
    public void ExposesEveryPanel()
    {
        using var viewModel = CreateViewModel();

        viewModel.Entry.ShouldNotBeNull();
        viewModel.History.ShouldNotBeNull();
        viewModel.Trend.ShouldNotBeNull();
        viewModel.Storage.ShouldNotBeNull();
        viewModel.Export.ShouldNotBeNull();
    }

    [Fact]
    public async Task DisposeStopsRespondingToSaves()
    {
        var viewModel = CreateViewModel();
        await viewModel.InitializeAsync(CancellationToken.None);
        viewModel.Dispose();

        GivenReadings([.. ReadingFactory.CreateDailySeries(5)]);
        await viewModel.Entry.SaveAsync(CancellationToken.None);

        viewModel.History.Readings.ShouldBeEmpty();
    }

    [Fact]
    public void ConstructorRejectsNullDependencies()
    {
        var entry = new ReadingEntryViewModel(new AddReadingUseCase(_repository, _clock), _clock);
        var history = new HistoryViewModel(
            new GetReadingHistoryUseCase(_repository, _clock),
            new RetractReadingUseCase(_repository, _clock));
        var trend = new TrendViewModel(new GetTrendUseCase(_repository, _clock));
        var storage = new StorageLocationViewModel(_location);
        var export = CreateExport();

        Should.Throw<ArgumentNullException>(() => new DashboardViewModel(null!, history, trend, storage, export));
        Should.Throw<ArgumentNullException>(() => new DashboardViewModel(entry, null!, trend, storage, export));
        Should.Throw<ArgumentNullException>(() => new DashboardViewModel(entry, history, null!, storage, export));
        Should.Throw<ArgumentNullException>(() => new DashboardViewModel(entry, history, trend, null!, export));
        Should.Throw<ArgumentNullException>(() => new DashboardViewModel(entry, history, trend, storage, null!));
    }
}
