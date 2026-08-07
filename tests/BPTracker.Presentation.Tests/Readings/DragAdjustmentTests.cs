using BPTracker.Presentation.Readings;

namespace BPTracker.Presentation.Tests.Readings;

public sealed class DragAdjustmentTests
{
    private static DragAdjustment Create(int start = 120) => DragAdjustment.For(start, 50, 300);

    [Fact]
    public void AGestureThatHasNotMovedLeavesTheValueAlone() =>
        Create().ValueAt(0, 0).ShouldBe(120);

    [Fact]
    public void SlidingRightIncreasesByOneStepPerInterval() =>
        Create().ValueAt(DragAdjustment.PixelsPerStep * 10, 0).ShouldBe(130);

    [Fact]
    public void SlidingLeftDecreases() =>
        Create().ValueAt(-DragAdjustment.PixelsPerStep * 10, 0).ShouldBe(110);

    [Fact]
    public void SlidingUpIncreases() =>
        Create().ValueAt(0, -DragAdjustment.PixelsPerStep * 5).ShouldBe(125);

    [Fact]
    public void SlidingDownDecreases() =>
        Create().ValueAt(0, DragAdjustment.PixelsPerStep * 5).ShouldBe(115);

    [Fact]
    public void MovementIsMeasuredFromTheStartSoRoundingCannotAccumulate()
    {
        var gesture = Create();

        gesture.ValueAt(1, 0).ShouldBe(120);
        gesture.ValueAt(2, 0).ShouldBe(120);
        gesture.ValueAt(DragAdjustment.PixelsPerStep * 3, 0).ShouldBe(123);
    }

    [Fact]
    public void TheValueNeverLeavesTheRange()
    {
        var gesture = Create();

        gesture.ValueAt(100_000, 0).ShouldBe(300);
        gesture.ValueAt(-100_000, 0).ShouldBe(50);
    }

    [Fact]
    public void AStartValueOutsideTheRangeIsPulledIn() =>
        DragAdjustment.For(500, 50, 300).StartValue.ShouldBe(300);

    [Fact]
    public void AnInvertedRangeIsRejected() =>
        Should.Throw<ArgumentOutOfRangeException>(() => DragAdjustment.For(120, 300, 50));
}
