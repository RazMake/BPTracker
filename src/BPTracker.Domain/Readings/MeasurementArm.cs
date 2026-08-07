namespace BPTracker.Domain.Readings;

/// <summary>Which arm the cuff was on. Readings can differ measurably between arms.</summary>
public enum MeasurementArm
{
    /// <summary>Not recorded.</summary>
    Unspecified,

    /// <summary>Left arm.</summary>
    Left,

    /// <summary>Right arm.</summary>
    Right,
}
