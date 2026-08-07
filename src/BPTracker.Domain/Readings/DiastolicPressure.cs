namespace BPTracker.Domain.Readings;

/// <summary>
/// Diastolic (lower) blood pressure in millimetres of mercury.
/// </summary>
public readonly record struct DiastolicPressure
{
    /// <summary>Lowest value accepted as a plausible human measurement.</summary>
    public const int Minimum = 30;

    /// <summary>Highest value accepted as a plausible human measurement.</summary>
    public const int Maximum = 200;

    private DiastolicPressure(int value) => MmHg = value;

    /// <summary>The pressure in mmHg.</summary>
    public int MmHg { get; }

    /// <summary>Creates a validated diastolic pressure.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the plausible range.</exception>
    public static DiastolicPressure From(int mmHg) =>
        TryFrom(mmHg, out var pressure)
            ? pressure
            : throw new ArgumentOutOfRangeException(
                nameof(mmHg),
                mmHg,
                $"Diastolic pressure must be between {Minimum} and {Maximum} mmHg.");

    /// <summary>Attempts to create a validated diastolic pressure without throwing.</summary>
    public static bool TryFrom(int mmHg, out DiastolicPressure pressure)
    {
        if (mmHg is < Minimum or > Maximum)
        {
            pressure = default;
            return false;
        }

        pressure = new DiastolicPressure(mmHg);
        return true;
    }

    /// <inheritdoc />
    public override string ToString() => MmHg.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
