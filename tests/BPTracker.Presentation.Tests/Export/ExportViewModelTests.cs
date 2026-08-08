using BPTracker.Application.Abstractions;
using BPTracker.Application.Readings;
using BPTracker.Presentation.Export;
using BPTracker.TestSupport;
using NSubstitute;

namespace BPTracker.Presentation.Tests.Export;

public sealed class ExportViewModelTests
{
    private readonly IReadingRepository _repository = Substitute.For<IReadingRepository>();
    private readonly IExportRenderer _renderer = Substitute.For<IExportRenderer>();
    private readonly TestClock _clock = new();

    private ExportViewModel CreateViewModel() => new(
        new GetReadingHistoryUseCase(_repository, _clock),
        _renderer,
        _clock);

    private void GivenReadings(params Domain.Readings.BloodPressureReading[] readings) =>
        _repository.GetRangeAsync(
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>()).Returns(readings);

    private void GivenTheRendererSaves(string path)
    {
        _renderer.SaveCsvAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(path);
        _renderer.SaveChartImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(path);
        _renderer.SaveTableImageAsync(Arg.Any<string>(), Arg.Any<ExportTable>(), Arg.Any<CancellationToken>())
            .Returns(path);
    }

    [Fact]
    public async Task CsvExportHandsTheRendererTheWholeFile()
    {
        GivenReadings(ReadingFactory.Create(134, 87));
        GivenTheRendererSaves(@"C:\out\readings.csv");
        var viewModel = CreateViewModel();

        await viewModel.ExportCsvAsync(CancellationToken.None);

        await _renderer.Received(1).SaveCsvAsync(
            Arg.Any<string>(),
            Arg.Is<string>(csv => csv != null && csv.Contains("Date,Time,Sys,Dia,Tag", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChartExportAsksForAPng()
    {
        GivenReadings(ReadingFactory.Create());
        GivenTheRendererSaves(@"C:\out\chart.png");
        var viewModel = CreateViewModel();

        await viewModel.ExportChartAsync(CancellationToken.None);

        await _renderer.Received(1).SaveChartImageAsync(
            Arg.Is<string>(name => name != null && name.EndsWith(".png", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DataExportHandsTheRendererTheTable()
    {
        GivenReadings([.. ReadingFactory.CreateDailySeries(3)]);
        GivenTheRendererSaves(@"C:\out\readings.png");
        var viewModel = CreateViewModel();

        await viewModel.ExportDataAsync(CancellationToken.None);

        await _renderer.Received(1).SaveTableImageAsync(
            Arg.Any<string>(),
            Arg.Is<ExportTable>(table => table != null && table.Rows.Count == 3),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NamesTheFileAfterTheKindAndTheMoment()
    {
        GivenReadings(ReadingFactory.Create());
        GivenTheRendererSaves(@"C:\out\readings.csv");
        var viewModel = CreateViewModel();

        await viewModel.ExportCsvAsync(CancellationToken.None);

        var expected = $"bptracker-readings-{TestClock.DefaultNow.LocalDateTime:yyyyMMdd-HHmm}.csv";
        await _renderer.Received(1).SaveCsvAsync(expected, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReportsWhereTheFileWent()
    {
        GivenReadings(ReadingFactory.Create());
        GivenTheRendererSaves(Path.Combine("out", "readings.csv"));
        var viewModel = CreateViewModel();

        await viewModel.ExportCsvAsync(CancellationToken.None);

        viewModel.StatusMessage.ShouldBe("Saved readings.csv");
    }

    [Fact]
    public async Task SaysSoWhenTheUserBacksOutOfTheSaveDialog()
    {
        GivenReadings(ReadingFactory.Create());
        _renderer.SaveCsvAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);
        var viewModel = CreateViewModel();

        await viewModel.ExportCsvAsync(CancellationToken.None);

        viewModel.StatusMessage.ShouldBe("Export cancelled.");
    }

    [Fact]
    public async Task RefusesToExportAnEmptyHistory()
    {
        var viewModel = CreateViewModel();

        await viewModel.ExportCsvAsync(CancellationToken.None);

        viewModel.StatusMessage.ShouldBe("There is nothing to export yet.");
        await _renderer.DidNotReceiveWithAnyArgs().SaveCsvAsync(default!, default!, default);
    }

    [Fact]
    public async Task ReportsAFailureToWriteRatherThanThrowing()
    {
        GivenReadings(ReadingFactory.Create());
        _renderer.SaveCsvAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<string?>(_ => throw new IOException("the disk is full"));
        var viewModel = CreateViewModel();

        await viewModel.ExportCsvAsync(CancellationToken.None);

        viewModel.StatusMessage.ShouldBe("Could not export: the disk is full");
    }

    [Fact]
    public async Task IsNotBusyOnceAnExportFails()
    {
        GivenReadings(ReadingFactory.Create());
        _renderer.SaveCsvAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<string?>(_ => throw new UnauthorizedAccessException("no"));
        var viewModel = CreateViewModel();

        await viewModel.ExportCsvAsync(CancellationToken.None);

        viewModel.IsBusy.ShouldBeFalse();
    }

    [Fact]
    public void ConstructorRejectsNullDependencies()
    {
        var history = new GetReadingHistoryUseCase(_repository, _clock);

        Should.Throw<ArgumentNullException>(() => new ExportViewModel(null!, _renderer, _clock));
        Should.Throw<ArgumentNullException>(() => new ExportViewModel(history, null!, _clock));
        Should.Throw<ArgumentNullException>(() => new ExportViewModel(history, _renderer, null!));
    }
}
