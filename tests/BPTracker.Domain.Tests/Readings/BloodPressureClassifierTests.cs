using BPTracker.Domain.Readings;

namespace BPTracker.Domain.Tests.Readings;

public sealed class BloodPressureClassifierTests
{
    [Theory]
    // Hypotension takes precedence over the normal band.
    [InlineData(85, 70, BloodPressureCategory.Hypotension)]
    [InlineData(110, 55, BloodPressureCategory.Hypotension)]
    // Normal.
    [InlineData(110, 70, BloodPressureCategory.Normal)]
    [InlineData(119, 79, BloodPressureCategory.Normal)]
    // Elevated: systolic 120-129 with diastolic still below 80.
    [InlineData(120, 79, BloodPressureCategory.Elevated)]
    [InlineData(129, 60, BloodPressureCategory.Elevated)]
    // Stage 1: systolic 130-139 OR diastolic 80-89.
    [InlineData(130, 70, BloodPressureCategory.HypertensionStage1)]
    [InlineData(125, 85, BloodPressureCategory.HypertensionStage1)]
    [InlineData(139, 89, BloodPressureCategory.HypertensionStage1)]
    // Stage 2: systolic >= 140 OR diastolic >= 90.
    [InlineData(140, 70, BloodPressureCategory.HypertensionStage2)]
    [InlineData(135, 95, BloodPressureCategory.HypertensionStage2)]
    [InlineData(180, 120, BloodPressureCategory.HypertensionStage2)]
    // Crisis: systolic > 180 OR diastolic > 120.
    [InlineData(181, 70, BloodPressureCategory.HypertensiveCrisis)]
    [InlineData(160, 121, BloodPressureCategory.HypertensiveCrisis)]
    [InlineData(200, 130, BloodPressureCategory.HypertensiveCrisis)]
    public void ClassifyAssignsTheExpectedCategory(int systolic, int diastolic, BloodPressureCategory expected)
    {
        var category = BloodPressureClassifier.Classify(
            SystolicPressure.From(systolic),
            DiastolicPressure.From(diastolic));

        category.ShouldBe(expected);
    }

    [Fact]
    public void CrisisOutranksHypotensionWhenBothCouldApply()
    {
        // Diastolic above 120 with a low-ish systolic is still a crisis, not hypotension.
        var category = BloodPressureClassifier.Classify(
            SystolicPressure.From(150),
            DiastolicPressure.From(125));

        category.ShouldBe(BloodPressureCategory.HypertensiveCrisis);
    }
}
