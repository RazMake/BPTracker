using BPTracker.Domain.Readings;
using BPTracker.Presentation.Readings;

namespace BPTracker.Presentation.Tests.Readings;

public sealed class BloodPressureCategoryNameTests
{
    [Theory]
    [InlineData(BloodPressureCategory.Hypotension, "Hypotension")]
    [InlineData(BloodPressureCategory.Normal, "Normal")]
    [InlineData(BloodPressureCategory.Elevated, "Elevated")]
    [InlineData(BloodPressureCategory.HypertensionStage1, "Hypertension stage 1")]
    [InlineData(BloodPressureCategory.HypertensionStage2, "Hypertension stage 2")]
    [InlineData(BloodPressureCategory.HypertensiveCrisis, "Hypertensive crisis")]
    public void EveryCategoryHasWordsRatherThanARunTogetherEnumName(
        BloodPressureCategory category,
        string expected) =>
        BloodPressureCategoryName.For(category).ShouldBe(expected);

    [Fact]
    public void NoCategoryYetReadsAsNothing() =>
        BloodPressureCategoryName.For(null).ShouldBeEmpty();

    [Fact]
    public void AnUnrecognisedValueReadsAsNothing() =>
        BloodPressureCategoryName.For((BloodPressureCategory)99).ShouldBeEmpty();

    [Fact]
    public void NoNameIsTheRawEnumName()
    {
        var names = Enum.GetValues<BloodPressureCategory>()
            .Select(BloodPressureCategoryName.For)
            .ToArray();

        names.ShouldAllBe(name => !string.IsNullOrWhiteSpace(name));
        names.ShouldNotContain(nameof(BloodPressureCategory.HypertensionStage1));
    }
}
