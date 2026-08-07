using BPTracker.Presentation.Charts;

namespace BPTracker.Presentation.Tests.Charts;

public sealed class PressureBandsTests
{
    [Fact]
    public void ThereIsOneCorridorPerSeries()
    {
        var corridors = PressureBands.For(40, 200);

        corridors.Count.ShouldBe(2);
        corridors.Select(band => band.Label).ToArray()
            .ShouldBe([PressureBands.DiastolicLabel, PressureBands.SystolicLabel]);
    }

    [Fact]
    public void EachCorridorCoversItsOwnNormalRange()
    {
        var corridors = PressureBands.For(40, 200);

        corridors[0].Lowest.ShouldBe(60);
        corridors[0].Highest.ShouldBe(80);
        corridors[1].Lowest.ShouldBe(90);
        corridors[1].Highest.ShouldBe(120);
    }

    [Fact]
    public void EveryCorridorSaysWhichSeriesItIsFor() =>
        PressureBands.For(40, 200).ShouldAllBe(band => band.Label.Contains("NORMAL"));

    [Fact]
    public void NothingOutsideTheNormalRangesIsShaded() =>
        PressureBands.For(40, 200).Sum(band => band.Highest - band.Lowest).ShouldBe(50);

    [Fact]
    public void ACorridorIsClippedToTheAxis()
    {
        var corridors = PressureBands.For(100, 200);

        corridors.Count.ShouldBe(1);
        corridors[0].Lowest.ShouldBe(100);
        corridors[0].Highest.ShouldBe(120);
        corridors[0].Label.ShouldBe(PressureBands.SystolicLabel);
    }

    [Fact]
    public void ACorridorEntirelyOutsideTheAxisIsDropped() =>
        PressureBands.For(130, 200).ShouldBeEmpty();

    [Fact]
    public void AnAxisTouchingOnlyTheEdgeOfACorridorIsDropped() =>
        PressureBands.For(120, 200).ShouldBeEmpty();

    [Theory]
    [InlineData(100, 100)]
    [InlineData(120, 100)]
    public void AnEmptyOrInvertedAxisIsRejected(int lowest, int highest) =>
        Should.Throw<ArgumentOutOfRangeException>(() => PressureBands.For(lowest, highest));
}
