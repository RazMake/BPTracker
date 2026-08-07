using BPTracker.Domain.Readings;
using BPTracker.TestSupport;

namespace BPTracker.Domain.Tests.Readings;

public sealed class BloodPressureReadingTests
{
    [Fact]
    public void CreateStoresSuppliedValues()
    {
        var measuredAt = new DateTimeOffset(2026, 2, 1, 7, 0, 0, TimeSpan.FromHours(2));
        var reading = BloodPressureReading.Create(
            SystolicPressure.From(128),
            DiastolicPressure.From(82),
            measuredAt,
            TestClock.DefaultNow);

        reading.Systolic.MmHg.ShouldBe(128);
        reading.Diastolic.MmHg.ShouldBe(82);
        reading.MeasuredAt.ShouldBe(measuredAt);
        reading.UpdatedAtUtc.ShouldBe(TestClock.DefaultNow);
        reading.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public void CreateGeneratesSortableIdentityWhenNoneSupplied()
    {
        var first = ReadingFactory.Create();
        var second = ReadingFactory.Create();

        first.Id.ShouldNotBe(Guid.Empty);
        first.Id.ShouldNotBe(second.Id);
    }

    [Fact]
    public void CreateHonoursExplicitIdentity()
    {
        var id = Guid.CreateVersion7();
        ReadingFactory.Create(id: id).Id.ShouldBe(id);
    }

    [Theory]
    [InlineData(120, 120)]
    [InlineData(80, 120)]
    public void CreateRejectsSystolicNotAboveDiastolic(int systolic, int diastolic) =>
        Should.Throw<ArgumentException>(() => BloodPressureReading.Create(
            SystolicPressure.From(systolic),
            DiastolicPressure.From(diastolic),
            TestClock.DefaultNow,
            TestClock.DefaultNow));

    [Fact]
    public void PulsePressureIsTheDifferenceBetweenPressures() =>
        ReadingFactory.Create(130, 85).PulsePressure.ShouldBe(45);

    [Fact]
    public void MeanArterialPressureUsesTheStandardEstimate() =>
        ReadingFactory.Create(120, 90).MeanArterialPressure.ShouldBe(100, 0.001);

    [Fact]
    public void CategoryDelegatesToTheClassifier() =>
        ReadingFactory.Create(190, 100).Category.ShouldBe(BloodPressureCategory.HypertensiveCrisis);

    [Fact]
    public void WithContextReplacesContextAndStampsUpdatedTime()
    {
        var reading = ReadingFactory.Create();
        var later = TestClock.DefaultNow.AddHours(1);

        var updated = reading.WithContext(
            new MeasurementContext { Arm = MeasurementArm.Left, Note = "  after walk  " },
            later);

        updated.Context.Arm.ShouldBe(MeasurementArm.Left);
        updated.Context.Note.ShouldBe("after walk");
        updated.UpdatedAtUtc.ShouldBe(later);
        updated.Id.ShouldBe(reading.Id);
    }

    [Fact]
    public void RetractMarksDeletedAndStampsUpdatedTime()
    {
        var later = TestClock.DefaultNow.AddMinutes(5);
        var retracted = ReadingFactory.Create().Retract(later);

        retracted.IsDeleted.ShouldBeTrue();
        retracted.UpdatedAtUtc.ShouldBe(later);
    }

    [Fact]
    public void ResolveConflictPrefersTheNewerCopy()
    {
        var id = Guid.CreateVersion7();
        var older = ReadingFactory.Create(120, 80, updatedAtUtc: TestClock.DefaultNow, id: id);
        var newer = ReadingFactory.Create(140, 90, updatedAtUtc: TestClock.DefaultNow.AddMinutes(1), id: id);

        BloodPressureReading.ResolveConflict(older, newer).ShouldBe(newer);
        BloodPressureReading.ResolveConflict(newer, older).ShouldBe(newer);
    }

    [Fact]
    public void ResolveConflictPrefersRetractionOnTie()
    {
        var id = Guid.CreateVersion7();
        var live = ReadingFactory.Create(id: id);
        var retracted = ReadingFactory.Create(id: id).Retract(TestClock.DefaultNow);

        BloodPressureReading.ResolveConflict(retracted, live).ShouldBe(retracted);
        BloodPressureReading.ResolveConflict(live, retracted).ShouldBe(retracted);
    }

    [Fact]
    public void ResolveConflictRejectsDifferentReadings() =>
        Should.Throw<ArgumentException>(() => BloodPressureReading.ResolveConflict(
            ReadingFactory.Create(),
            ReadingFactory.Create()));

    [Fact]
    public void ResolveConflictRejectsNullArguments()
    {
        var reading = ReadingFactory.Create();
        Should.Throw<ArgumentNullException>(() => BloodPressureReading.ResolveConflict(null!, reading));
        Should.Throw<ArgumentNullException>(() => BloodPressureReading.ResolveConflict(reading, null!));
    }
}
