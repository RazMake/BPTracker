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
            new MeasurementContext
            {
                Arm = MeasurementArm.Right,
                Position = BodyPosition.Sitting,
                Note = "after coffee",
            });

        ReadingLineSerializer.TryParse(ReadingLineSerializer.ToLine(original), out var parsed)
            .ShouldBeTrue();

        parsed.Id.ShouldBe(original.Id);
        parsed.Systolic.MmHg.ShouldBe(134);
        parsed.Diastolic.MmHg.ShouldBe(87);
        parsed.Context.Arm.ShouldBe(MeasurementArm.Right);
        parsed.Context.Position.ShouldBe(BodyPosition.Sitting);
        parsed.Context.Note.ShouldBe("after coffee");
        parsed.UpdatedAtUtc.ShouldBe(original.UpdatedAtUtc);
        parsed.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public void RoundTripPreservesTheOriginalOffset()
    {
        var measuredAt = new DateTimeOffset(2026, 5, 4, 9, 15, 0, TimeSpan.FromHours(3));
        var line = ReadingLineSerializer.ToLine(ReadingFactory.Create(measuredAt: measuredAt));

        ReadingLineSerializer.TryParse(line, out var parsed).ShouldBeTrue();

        parsed.MeasuredAt.ToUniversalTime().ShouldBe(measuredAt.ToUniversalTime());
        parsed.MeasuredAt.Offset.ShouldBe(TimeSpan.FromHours(3));
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
            .ShouldContain("\"Systolic\":135");

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
            {"Id":"0197f0a0-0000-7000-8000-000000000001","Systolic":{{systolic}},"Diastolic":{{diastolic}},"MeasuredAt":"2026-05-04T09:15:00.0000000+00:00","Arm":"Left","Position":"Sitting","UpdatedAt":"2026-05-04T09:15:00.0000000+00:00","Deleted":false}
            """;

        ReadingLineSerializer.TryParse(line, out _).ShouldBeFalse();
    }

    [Fact]
    public void UnknownEnumValuesFallBackRatherThanFailing()
    {
        var line = """
            {"Id":"0197f0a0-0000-7000-8000-000000000001","Systolic":120,"Diastolic":80,"MeasuredAt":"2026-05-04T09:15:00.0000000+00:00","Arm":"Tentacle","Position":"Hovering","UpdatedAt":"2026-05-04T09:15:00.0000000+00:00","Deleted":false}
            """;

        ReadingLineSerializer.TryParse(line, out var parsed).ShouldBeTrue();
        parsed.Context.Arm.ShouldBe(MeasurementArm.Unspecified);
        parsed.Context.Position.ShouldBe(BodyPosition.Unspecified);
    }

    [Fact]
    public void ToLineRejectsNull() =>
        Should.Throw<ArgumentNullException>(() => ReadingLineSerializer.ToLine(null!));
}
