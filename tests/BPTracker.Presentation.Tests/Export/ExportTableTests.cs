using BPTracker.Presentation.Export;
using BPTracker.TestSupport;

namespace BPTracker.Presentation.Tests.Export;

public sealed class ExportTableTests
{
    [Fact]
    public void HasTheFiveColumnsTheJournalStores() =>
        ExportTable.Columns.Select(column => column.Header)
            .ShouldBe(["Date", "Time", "Sys", "Dia", "Tag"]);

    [Fact]
    public void LaysOutOneRowPerReading() =>
        ExportTable.For(ReadingFactory.CreateDailySeries(4)).Rows.Count.ShouldBe(4);

    [Fact]
    public void OrdersOldestFirstSoItReadsLikeTheChart()
    {
        var table = ExportTable.For([
            ReadingFactory.Create(measuredAt: TestClock.DefaultNow),
            ReadingFactory.Create(measuredAt: TestClock.DefaultNow.AddDays(-2)),
        ]);

        table.Rows[0][0].ShouldBe(TestClock.DefaultNow.AddDays(-2).LocalDateTime.ToString("yyyy-MM-dd", null));
    }

    [Fact]
    public void SplitsAReadingIntoDateTimeAndPressures()
    {
        var measuredAt = new DateTimeOffset(2026, 5, 4, 9, 15, 0, TimeSpan.Zero).ToLocalTime();

        var cells = ExportTable.For([ReadingFactory.Create(134, 87, measuredAt, tag: "after a run")]).Rows[0];

        cells[0].ShouldBe(measuredAt.LocalDateTime.ToString("yyyy-MM-dd", null));
        cells[1].ShouldBe(measuredAt.LocalDateTime.ToString("HH:mm", null));
        cells[2].ShouldBe("134");
        cells[3].ShouldBe("87");
        cells[4].ShouldBe("after a run");
    }

    [Fact]
    public void AMissingTagBecomesAnEmptyCell() =>
        ExportTable.For([ReadingFactory.Create()]).Rows[0][4].ShouldBeEmpty();

    [Fact]
    public void GrowsTallerWithMoreRows()
    {
        var one = ExportTable.For(ReadingFactory.CreateDailySeries(1)).Height;
        var many = ExportTable.For(ReadingFactory.CreateDailySeries(3)).Height;

        (many - one).ShouldBe(2 * ExportTable.RowHeight);
    }

    [Fact]
    public void IsAsWideAsItsColumnsPlusMargins() =>
        ExportTable.Width.ShouldBe((2 * ExportTable.Margin) + ExportTable.Columns.Sum(column => column.Width));

    [Fact]
    public void TheFirstColumnStartsAtTheMargin() =>
        ExportTable.LeftOf(0).ShouldBe(ExportTable.Margin);

    [Fact]
    public void EachColumnStartsWhereTheLastEnded() =>
        ExportTable.LeftOf(2).ShouldBe(
            ExportTable.Margin + ExportTable.Columns[0].Width + ExportTable.Columns[1].Width);

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    public void LeftOfRejectsAColumnOutsideTheTable(int column) =>
        Should.Throw<ArgumentOutOfRangeException>(() => ExportTable.LeftOf(column));

    [Fact]
    public void ForRejectsNull() =>
        Should.Throw<ArgumentNullException>(() => ExportTable.For(null!));

    [Fact]
    public void AnEmptyHistoryProducesNoRows() =>
        ExportTable.For([]).Rows.ShouldBeEmpty();
}
