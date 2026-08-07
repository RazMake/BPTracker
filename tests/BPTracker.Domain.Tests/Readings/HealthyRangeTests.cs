using BPTracker.Domain.Readings;

namespace BPTracker.Domain.Tests.Readings;

public sealed class HealthyRangeTests
{
    [Theory]
    [InlineData(89, PressureZone.Low)]
    [InlineData(90, PressureZone.Healthy)]
    [InlineData(119, PressureZone.Healthy)]
    [InlineData(120, PressureZone.High)]
    public void SystolicZoneChangesAtTheBandEdges(double mmHg, PressureZone expected) =>
        HealthyRange.Systolic.Zone(mmHg).ShouldBe(expected);

    [Theory]
    [InlineData(59, PressureZone.Low)]
    [InlineData(60, PressureZone.Healthy)]
    [InlineData(79, PressureZone.Healthy)]
    [InlineData(80, PressureZone.High)]
    public void DiastolicZoneChangesAtTheBandEdges(double mmHg, PressureZone expected) =>
        HealthyRange.Diastolic.Zone(mmHg).ShouldBe(expected);

    [Fact]
    public void InterpolatedValueJustInsideTheUpperEdgeIsStillHealthy() =>
        HealthyRange.Systolic.Zone(119.99).ShouldBe(PressureZone.Healthy);

    [Fact]
    public void LowerEdgeAgreesWithTheClassifier()
    {
        var range = HealthyRange.Systolic;

        BloodPressureClassifier
            .Classify(SystolicPressure.From(range.Lowest), DiastolicPressure.From(60))
            .ShouldBe(BloodPressureCategory.Normal);

        BloodPressureClassifier
            .Classify(SystolicPressure.From(range.Lowest - 1), DiastolicPressure.From(60))
            .ShouldBe(BloodPressureCategory.Hypotension);
    }

    [Fact]
    public void UpperEdgeAgreesWithTheClassifier()
    {
        var range = HealthyRange.Systolic;

        BloodPressureClassifier
            .Classify(SystolicPressure.From(range.TooHigh - 1), DiastolicPressure.From(70))
            .ShouldBe(BloodPressureCategory.Normal);

        BloodPressureClassifier
            .Classify(SystolicPressure.From(range.TooHigh), DiastolicPressure.From(70))
            .ShouldBe(BloodPressureCategory.Elevated);
    }
}
