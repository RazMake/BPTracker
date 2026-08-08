using BPTracker.Presentation.Export;
using BPTracker.TestSupport;

namespace BPTracker.Presentation.Tests.Export;

public sealed class ExportCsvTests
{
    private static string[] Lines(params Domain.Readings.BloodPressureReading[] readings) =>
        ExportCsv.Build(ExportTable.For(readings))
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

    [Fact]
    public void StartsWithTheColumnHeaders() =>
        Lines(ReadingFactory.Create())[0].ShouldBe("Date,Time,Sys,Dia,Tag");

    [Fact]
    public void WritesOneLinePerReading() =>
        Lines([.. ReadingFactory.CreateDailySeries(3)]).Length.ShouldBe(4);

    [Fact]
    public void WritesTheHeaderEvenWithNoReadings() =>
        Lines().Length.ShouldBe(1);

    [Fact]
    public void PutsThePressuresInTheirOwnColumns() =>
        Lines(ReadingFactory.Create(134, 87))[1].ShouldContain(",134,87,");

    [Fact]
    public void QuotesATagContainingAComma() =>
        Lines(ReadingFactory.Create(tag: "after a run, uphill"))[1]
            .ShouldEndWith("\"after a run, uphill\"");

    [Fact]
    public void DoublesQuotesInsideATag() =>
        Lines(ReadingFactory.Create(tag: "the \"good\" cuff"))[1]
            .ShouldEndWith("\"the \"\"good\"\" cuff\"");

    [Theory]
    [InlineData("=1+1")]
    [InlineData("+cmd")]
    [InlineData("-2")]
    [InlineData("@SUM(A1)")]
    public void DefusesATagASpreadsheetWouldTreatAsAFormula(string tag) =>
        Lines(ReadingFactory.Create(tag: tag))[1].ShouldEndWith($"\"'{tag}\"");

    [Fact]
    public void LeavesAnOrdinaryTagUnquoted() =>
        Lines(ReadingFactory.Create(tag: "morning"))[1].ShouldEndWith(",morning");

    [Fact]
    public void BuildRejectsNull() =>
        Should.Throw<ArgumentNullException>(() => ExportCsv.Build(null!));
}
