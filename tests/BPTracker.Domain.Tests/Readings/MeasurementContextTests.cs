using BPTracker.Domain.Readings;

namespace BPTracker.Domain.Tests.Readings;

public sealed class MeasurementContextTests
{
    [Fact]
    public void NoneRecordsNothing()
    {
        var context = MeasurementContext.None;

        context.Arm.ShouldBe(MeasurementArm.Unspecified);
        context.Position.ShouldBe(BodyPosition.Unspecified);
        context.Tag.ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void NormalisedCollapsesBlankTagsToNull(string? tag) =>
        new MeasurementContext { Tag = tag }.Normalised().Tag.ShouldBeNull();

    [Fact]
    public void NormalisedTrimsSurroundingWhitespace() =>
        new MeasurementContext { Tag = "  before breakfast  " }.Normalised().Tag
            .ShouldBe("before breakfast");

    [Fact]
    public void NormalisedPreservesOtherFields()
    {
        var context = new MeasurementContext
        {
            Arm = MeasurementArm.Right,
            Position = BodyPosition.Sitting,
            Tag = "ok",
        }.Normalised();

        context.Arm.ShouldBe(MeasurementArm.Right);
        context.Position.ShouldBe(BodyPosition.Sitting);
    }

    [Fact]
    public void NormalisedAcceptsTagAtTheLengthLimit()
    {
        var tag = new string('a', MeasurementContext.MaxTagLength);
        new MeasurementContext { Tag = tag }.Normalised().Tag.ShouldBe(tag);
    }

    [Fact]
    public void NormalisedRejectsOverlongTag()
    {
        var tag = new string('a', MeasurementContext.MaxTagLength + 1);
        Should.Throw<InvalidOperationException>(() => new MeasurementContext { Tag = tag }.Normalised());
    }

    [Fact]
    public void ClampShortensATagThatIsTooLong() =>
        MeasurementContext.Clamp(new string('a', MeasurementContext.MaxTagLength + 50))
            .ShouldBe(new string('a', MeasurementContext.MaxTagLength));

    [Fact]
    public void ClampLeavesAnAcceptableTagAlone() =>
        MeasurementContext.Clamp("after a run").ShouldBe("after a run");

    [Fact]
    public void ClampLeavesNullAlone() =>
        MeasurementContext.Clamp(null).ShouldBeNull();
}
