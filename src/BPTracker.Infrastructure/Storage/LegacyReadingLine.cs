using System.Text.Json.Serialization;

namespace BPTracker.Infrastructure.Storage;

/// <summary>
/// The journal shape written before readings moved to Date, Time, Sys, Dia and Tag.
/// </summary>
/// <remarks>
/// Kept so a journal written by an older build still loads and can be rewritten into the current
/// shape. <c>Arm</c> and <c>Position</c> are deliberately absent: neither app ever asked for them,
/// so they are dropped on the way through rather than carried forward.
/// </remarks>
internal sealed record LegacyReadingLine
{
    public Guid Id { get; init; }

    public int Systolic { get; init; }

    public int Diastolic { get; init; }

    public string? MeasuredAt { get; init; }

    public string? Note { get; init; }

    public string? UpdatedAt { get; init; }

    public bool Deleted { get; init; }
}

[JsonSerializable(typeof(LegacyReadingLine))]
internal sealed partial class LegacyReadingJsonContext : JsonSerializerContext;
