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
        context.Note.ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void NormalisedCollapsesBlankNotesToNull(string? note) =>
        new MeasurementContext { Note = note }.Normalised().Note.ShouldBeNull();

    [Fact]
    public void NormalisedTrimsSurroundingWhitespace() =>
        new MeasurementContext { Note = "  before breakfast  " }.Normalised().Note
            .ShouldBe("before breakfast");

    [Fact]
    public void NormalisedPreservesOtherFields()
    {
        var context = new MeasurementContext
        {
            Arm = MeasurementArm.Right,
            Position = BodyPosition.Sitting,
            Note = "ok",
        }.Normalised();

        context.Arm.ShouldBe(MeasurementArm.Right);
        context.Position.ShouldBe(BodyPosition.Sitting);
    }

    [Fact]
    public void NormalisedAcceptsNoteAtTheLengthLimit()
    {
        var note = new string('a', MeasurementContext.MaxNoteLength);
        new MeasurementContext { Note = note }.Normalised().Note.ShouldBe(note);
    }

    [Fact]
    public void NormalisedRejectsOverlongNote()
    {
        var note = new string('a', MeasurementContext.MaxNoteLength + 1);
        Should.Throw<InvalidOperationException>(() => new MeasurementContext { Note = note }.Normalised());
    }
}
