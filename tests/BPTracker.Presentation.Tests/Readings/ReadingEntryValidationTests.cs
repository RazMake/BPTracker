using BPTracker.Domain.Readings;
using BPTracker.Presentation.Readings;

namespace BPTracker.Presentation.Tests.Readings;

public sealed class ReadingEntryValidationTests
{
    [Fact]
    public void ValidPairIsAcceptedAndClassified()
    {
        var validation = ReadingEntryValidation.Validate(128, 82);

        validation.IsValid.ShouldBeTrue();
        validation.Message.ShouldBeNull();
        validation.Category.ShouldBe(BloodPressureCategory.HypertensionStage1);
    }

    [Theory]
    [InlineData(SystolicPressure.Minimum - 1)]
    [InlineData(SystolicPressure.Maximum + 1)]
    public void OutOfRangeSystolicIsRejected(int systolic)
    {
        var validation = ReadingEntryValidation.Validate(systolic, 80);

        validation.IsValid.ShouldBeFalse();
        validation.Category.ShouldBeNull();
        validation.Message!.ShouldContain("Systolic");
    }

    [Theory]
    [InlineData(DiastolicPressure.Minimum - 1)]
    [InlineData(DiastolicPressure.Maximum + 1)]
    public void OutOfRangeDiastolicIsRejected(int diastolic)
    {
        var validation = ReadingEntryValidation.Validate(120, diastolic);

        validation.IsValid.ShouldBeFalse();
        validation.Message!.ShouldContain("Diastolic");
    }

    [Fact]
    public void SystolicMustExceedDiastolic()
    {
        var validation = ReadingEntryValidation.Validate(80, 90);

        validation.IsValid.ShouldBeFalse();
        validation.Message.ShouldBe("Systolic must be higher than diastolic.");
    }

    [Fact]
    public void EqualPressuresAreRejected() =>
        ReadingEntryValidation.Validate(100, 100).IsValid.ShouldBeFalse();
}
