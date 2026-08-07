namespace BPTracker.Domain.Readings;

/// <summary>
/// Systolic (upper) blood pressure in millimetres of mercury.
/// </summary>
public readonly record struct SystolicPressure
{
    /// <summary>Lowest value accepted as a plausible human measurement.</summary>
    public const int Minimum = 50;

    /// <summary>Highest value accepted as a plausible human measurement.</summary>
    public const int Maximum = 300;

    private SystolicPressure(int value) => MmHg = value;

    /// <summary>The pressure in mmHg.</summary>
    public int MmHg { get; }

    /// <summary>Creates a validated systolic pressure.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the plausible range.</exception>
    public static SystolicPressure From(int mmHg) =>
        TryFrom(mmHg, out var pressure)
            ? pressure
            : throw new ArgumentOutOfRangeException(
                nameof(mmHg),
                mmHg,
                $"Systolic pressure must be between {Minimum} and {Maximum} mmHg.");

    /// <summary>Attempts to create a validated systolic pressure without throwing.</summary>
    /// <remarks>Used by the entry screens so typing an incomplete number is not an exception.</remarks>
    public static bool TryFrom(int mmHg, out SystolicPressure pressure)
    {
        if (mmHg is < Minimum or > Maximum)
        {
            pressure = default;
            return false;
        }

        pressure = new SystolicPressure(mmHg);
        return true;
    }

    /// <inheritdoc />
    public override string ToString() => MmHg.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
