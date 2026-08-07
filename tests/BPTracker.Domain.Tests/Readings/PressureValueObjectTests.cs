using BPTracker.Domain.Readings;

namespace BPTracker.Domain.Tests.Readings;

public sealed class PressureValueObjectTests
{
    [Theory]
    [InlineData(SystolicPressure.Minimum)]
    [InlineData(120)]
    [InlineData(SystolicPressure.Maximum)]
    public void SystolicFromAcceptsValuesInsideRange(int value) =>
        SystolicPressure.From(value).MmHg.ShouldBe(value);

    [Theory]
    [InlineData(SystolicPressure.Minimum - 1)]
    [InlineData(SystolicPressure.Maximum + 1)]
    [InlineData(0)]
    [InlineData(-5)]
    public void SystolicFromRejectsValuesOutsideRange(int value) =>
        Should.Throw<ArgumentOutOfRangeException>(() => SystolicPressure.From(value));

    [Fact]
    public void SystolicTryFromReportsFailureWithoutThrowing()
    {
        SystolicPressure.TryFrom(SystolicPressure.Maximum + 1, out var pressure).ShouldBeFalse();
        pressure.ShouldBe(default);
    }

    [Fact]
    public void SystolicTryFromReportsSuccess()
    {
        SystolicPressure.TryFrom(130, out var pressure).ShouldBeTrue();
        pressure.MmHg.ShouldBe(130);
    }

    [Fact]
    public void SystolicToStringIsCultureInvariant() =>
        SystolicPressure.From(145).ToString().ShouldBe("145");

    [Theory]
    [InlineData(DiastolicPressure.Minimum)]
    [InlineData(80)]
    [InlineData(DiastolicPressure.Maximum)]
    public void DiastolicFromAcceptsValuesInsideRange(int value) =>
        DiastolicPressure.From(value).MmHg.ShouldBe(value);

    [Theory]
    [InlineData(DiastolicPressure.Minimum - 1)]
    [InlineData(DiastolicPressure.Maximum + 1)]
    public void DiastolicFromRejectsValuesOutsideRange(int value) =>
        Should.Throw<ArgumentOutOfRangeException>(() => DiastolicPressure.From(value));

    [Fact]
    public void DiastolicTryFromReportsFailureWithoutThrowing()
    {
        DiastolicPressure.TryFrom(DiastolicPressure.Minimum - 1, out var pressure).ShouldBeFalse();
        pressure.ShouldBe(default);
    }

    [Fact]
    public void DiastolicTryFromReportsSuccess()
    {
        DiastolicPressure.TryFrom(85, out var pressure).ShouldBeTrue();
        pressure.MmHg.ShouldBe(85);
    }

    [Fact]
    public void DiastolicToStringIsCultureInvariant() =>
        DiastolicPressure.From(92).ToString().ShouldBe("92");

    [Fact]
    public void PressuresWithSameValueAreEqual() =>
        SystolicPressure.From(120).ShouldBe(SystolicPressure.From(120));
}
