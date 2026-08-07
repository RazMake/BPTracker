namespace BPTracker.Domain.Readings;

/// <summary>
/// The band a single pressure value should stay inside, half-open as
/// <c>[<see cref="Lowest"/>, <see cref="TooHigh"/>)</c>.
/// </summary>
/// <remarks>
/// These are the same boundaries <see cref="BloodPressureClassifier"/> uses for its hypotension
/// and elevated bands. They live here so a chart and a category can never disagree about what
/// "healthy" means.
/// </remarks>
public readonly record struct HealthyRange
{
    private HealthyRange(int lowest, int tooHigh)
    {
        Lowest = lowest;
        TooHigh = tooHigh;
    }

    /// <summary>Healthy systolic pressure: 90 up to but not including 120 mmHg.</summary>
    public static HealthyRange Systolic { get; } = new(90, 120);

    /// <summary>Healthy diastolic pressure: 60 up to but not including 80 mmHg.</summary>
    public static HealthyRange Diastolic { get; } = new(60, 80);

    /// <summary>Lowest healthy value, in mmHg. Inclusive.</summary>
    public int Lowest { get; }

    /// <summary>Lowest value that is no longer healthy, in mmHg. Exclusive lower edge of "high".</summary>
    public int TooHigh { get; }

    /// <summary>Classifies a value against this band.</summary>
    /// <remarks>
    /// Takes a <see cref="double"/> because a chart interpolates between measurements, and the
    /// interpolated value decides where the line changes colour.
    /// </remarks>
    public PressureZone Zone(double mmHg)
    {
        if (mmHg < Lowest)
        {
            return PressureZone.Low;
        }

        return mmHg >= TooHigh ? PressureZone.High : PressureZone.Healthy;
    }
}
