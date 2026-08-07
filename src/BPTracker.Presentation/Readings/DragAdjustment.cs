namespace BPTracker.Presentation.Readings;

/// <summary>
/// Turns finger movement into a new value, so a number can be changed by sliding rather than by
/// tapping a step button once per mmHg.
/// </summary>
/// <remarks>
/// The gesture is measured from where it started, not from the previous update, so the value
/// follows the finger exactly and rounding never accumulates.
/// </remarks>
public readonly record struct DragAdjustment
{
    /// <summary>Device-independent pixels of movement worth one mmHg.</summary>
    public const double PixelsPerStep = 6d;

    private DragAdjustment(int startValue, int minimum, int maximum)
    {
        StartValue = startValue;
        Minimum = minimum;
        Maximum = maximum;
    }

    /// <summary>Value the gesture started from.</summary>
    public int StartValue { get; }

    /// <summary>Lowest value the gesture can reach.</summary>
    public int Minimum { get; }

    /// <summary>Highest value the gesture can reach.</summary>
    public int Maximum { get; }

    /// <summary>Starts a gesture.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maximum"/> is below <paramref name="minimum"/>.</exception>
    public static DragAdjustment For(int startValue, int minimum, int maximum)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximum, minimum);

        return new DragAdjustment(Math.Clamp(startValue, minimum, maximum), minimum, maximum);
    }

    /// <summary>
    /// The value after the finger has travelled the given total offset. Moving right or up
    /// increases, which matches how both a horizontal slider and a vertical dial are read.
    /// </summary>
    public int ValueAt(double totalX, double totalY)
    {
        var steps = (int)Math.Round((totalX - totalY) / PixelsPerStep, MidpointRounding.AwayFromZero);
        return Math.Clamp(StartValue + steps, Minimum, Maximum);
    }
}
