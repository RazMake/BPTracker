using System.Globalization;
using BPTracker.Domain.Readings;
using BPTracker.Infrastructure.Storage;
using BPTracker.TestSupport;

namespace BPTracker.Infrastructure.Tests.Storage;

public sealed class ReadingLineSerializerTests
{
    [Fact]
    public void RoundTripsEveryField()
    {
        var measuredAt = new DateTimeOffset(2026, 5, 4, 9, 15, 0, TimeSpan.FromHours(3));
        var original = BloodPressureReading.Create(
            SystolicPressure.From(134),
            DiastolicPressure.From(87),
            measuredAt,
            TestClock.DefaultNow,
            new MeasurementContext { Tag = "after coffee" });

        ReadingLineSerializer.TryParse(ReadingLineSerializer.ToLine(original), out var parsed)
            .ShouldBeTrue();

        parsed.Id.ShouldBe(original.Id);
        parsed.Systolic.MmHg.ShouldBe(134);
        parsed.Diastolic.MmHg.ShouldBe(87);
        parsed.Context.Tag.ShouldBe("after coffee");
        parsed.UpdatedAtUtc.ShouldBe(original.UpdatedAtUtc);
        parsed.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public void RoundTripPreservesTheInstantAsLocalTime()
    {
        var measuredAt = new DateTimeOffset(2026, 5, 4, 9, 15, 0, TimeSpan.FromHours(3));
        var line = ReadingLineSerializer.ToLine(ReadingFactory.Create(measuredAt: measuredAt));

        ReadingLineSerializer.TryParse(line, out var parsed).ShouldBeTrue();

        parsed.MeasuredAt.ShouldBe(measuredAt.ToLocalTime());
    }

    [Fact]
    public void WritesTheFiveFieldsTheUserCaresAbout()
    {
        var measuredAt = new DateTimeOffset(2026, 5, 4, 9, 15, 0, TimeSpan.Zero).ToLocalTime();
        var reading = ReadingFactory.Create(135, 85, measuredAt: measuredAt, tag: "after a run");

        var line = ReadingLineSerializer.ToLine(reading);

        line.ShouldContain($"\"Date\":\"{measuredAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}\"");
        line.ShouldContain($"\"Time\":\"{measuredAt.ToString("HH:mm", CultureInfo.InvariantCulture)}\"");
        line.ShouldContain("\"Sys\":135");
        line.ShouldContain("\"Dia\":85");
        line.ShouldContain("\"Tag\":\"after a run\"");
    }

    [Fact]
    public void DoesNotWriteArmOrPosition()
    {
        var line = ReadingLineSerializer.ToLine(ReadingFactory.Create());

        line.ShouldNotContain("Arm");
        line.ShouldNotContain("Position");
    }

    [Fact]
    public void RoundTripsARetractedReading()
    {
        var retracted = ReadingFactory.Create().Retract(TestClock.DefaultNow.AddMinutes(5));

        ReadingLineSerializer.TryParse(ReadingLineSerializer.ToLine(retracted), out var parsed)
            .ShouldBeTrue();

        parsed.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public void ProducesASingleLine() =>
        ReadingLineSerializer.ToLine(ReadingFactory.Create())
            .ShouldNotContain("\n");

    [Fact]
    public void LineIsReadableByAHuman() =>
        ReadingLineSerializer.ToLine(ReadingFactory.Create(135, 85))
            .ShouldContain("\"Sys\":135");

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json")]
    [InlineData("{\"Id\":\"")]
    [InlineData("{}")]
    public void RejectsUnusableLines(string line) =>
        ReadingLineSerializer.TryParse(line, out _).ShouldBeFalse();

    [Fact]
    public void RejectsALineTruncatedByAPartialSync()
    {
        var full = ReadingLineSerializer.ToLine(ReadingFactory.Create());
        var truncated = full[..(full.Length / 2)];

        ReadingLineSerializer.TryParse(truncated, out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData(400, 80)]
    [InlineData(120, 200)]
    [InlineData(80, 120)]
    public void RejectsImplausibleValues(int systolic, int diastolic)
    {
        var line = $$"""
            {"Date":"2026-05-04","Time":"09:15","Sys":{{systolic}},"Dia":{{diastolic}},"Id":"0197f0a0-0000-7000-8000-000000000001","UpdatedAt":"2026-05-04T09:15:00.0000000+00:00","Deleted":false}
            """;

        ReadingLineSerializer.TryParse(line, out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData("04/05/2026", "09:15")]
    [InlineData("2026-13-04", "09:15")]
    [InlineData("2026-05-04", "9:15 AM")]
    [InlineData("2026-05-04", "25:00")]
    public void RejectsUnparseableDatesAndTimes(string date, string time)
    {
        var line = $$"""
            {"Date":"{{date}}","Time":"{{time}}","Sys":120,"Dia":80,"Id":"0197f0a0-0000-7000-8000-000000000001","UpdatedAt":"2026-05-04T09:15:00.0000000+00:00","Deleted":false}
            """;

        ReadingLineSerializer.TryParse(line, out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AMissingTimeFallsBackToTheMorning(string time)
    {
        var line = $$"""
            {"Date":"2026-05-04","Time":"{{time}}","Sys":120,"Dia":80,"Id":"0197f0a0-0000-7000-8000-000000000001","UpdatedAt":"2026-05-04T09:15:00.0000000+00:00","Deleted":false}
            """;

        ReadingLineSerializer.TryParse(line, out var parsed).ShouldBeTrue();

        parsed.MeasuredAt.TimeOfDay.ShouldBe(TimeSpan.Parse(
            ReadingLineSerializer.DefaultTime,
            CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ReadsAJournalWrittenBeforeTheFormatChanged()
    {
        var line = """
            {"Id":"0197f0a0-0000-7000-8000-000000000001","Systolic":128,"Diastolic":82,"MeasuredAt":"2026-05-04T09:15:00.0000000+00:00","Arm":"Left","Position":"Sitting","Note":"after coffee","UpdatedAt":"2026-05-04T09:20:00.0000000+00:00","Deleted":false}
            """;

        ReadingLineSerializer.TryParse(line, out var parsed).ShouldBeTrue();

        parsed.Systolic.MmHg.ShouldBe(128);
        parsed.Diastolic.MmHg.ShouldBe(82);
        parsed.Context.Tag.ShouldBe("after coffee");
        parsed.MeasuredAt.ShouldBe(new DateTimeOffset(2026, 5, 4, 9, 15, 0, TimeSpan.Zero).ToLocalTime());
    }

    [Fact]
    public void ARetractedLegacyLineStaysRetracted()
    {
        var line = """
            {"Id":"0197f0a0-0000-7000-8000-000000000001","Systolic":128,"Diastolic":82,"MeasuredAt":"2026-05-04T09:15:00.0000000+00:00","UpdatedAt":"2026-05-04T09:20:00.0000000+00:00","Deleted":true}
            """;

        ReadingLineSerializer.TryParse(line, out var parsed).ShouldBeTrue();

        parsed.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public void RejectsALegacyLineWithAnUnusableTimestamp()
    {
        var line = """
            {"Id":"0197f0a0-0000-7000-8000-000000000001","Systolic":128,"Diastolic":82,"MeasuredAt":"whenever","UpdatedAt":"2026-05-04T09:20:00.0000000+00:00","Deleted":false}
            """;

        ReadingLineSerializer.TryParse(line, out _).ShouldBeFalse();
    }

    [Fact]
    public void ToLineRejectsNull() =>
        Should.Throw<ArgumentNullException>(() => ReadingLineSerializer.ToLine(null!));
}
