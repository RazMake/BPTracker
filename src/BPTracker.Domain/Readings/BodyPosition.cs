namespace BPTracker.Domain.Readings;

/// <summary>Posture during measurement, which materially affects the result.</summary>
public enum BodyPosition
{
    /// <summary>Not recorded.</summary>
    Unspecified,

    /// <summary>Seated, the reference posture for most guidelines.</summary>
    Sitting,

    /// <summary>Standing.</summary>
    Standing,

    /// <summary>Lying down.</summary>
    Lying,
}
