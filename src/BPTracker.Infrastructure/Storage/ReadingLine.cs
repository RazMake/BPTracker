using System.Text.Json.Serialization;

namespace BPTracker.Infrastructure.Storage;

/// <summary>
/// One reading as it appears on a single line of a journal file.
/// </summary>
/// <remarks>
/// The first five fields are the reading as the user thinks of it, in the order that reads best in
/// a text editor. The last three are bookkeeping: they are what lets two devices' journals merge
/// without a conflict, and what lets a retraction travel between them.
/// <para>
/// Every string is nullable because this is a wire type: a hand-edited or truncated line may leave
/// any of them out, and that has to parse to <see langword="false"/> rather than throw.
/// </para>
/// </remarks>
public sealed record ReadingLine
{
    /// <summary>Local calendar day of the measurement, <c>yyyy-MM-dd</c>.</summary>
    public string? Date { get; init; }

    /// <summary>Local time of the measurement, <c>HH:mm</c>. Blank falls back to 07:30.</summary>
    public string? Time { get; init; }

    /// <summary>Systolic pressure in mmHg.</summary>
    public int Sys { get; init; }

    /// <summary>Diastolic pressure in mmHg.</summary>
    public int Dia { get; init; }

    /// <summary>Optional short tag.</summary>
    public string? Tag { get; init; }

    /// <summary>Reading identity, so a correction replaces the reading instead of duplicating it.</summary>
    public Guid Id { get; init; }

    /// <summary>When the record last changed, ISO-8601 UTC. Drives last-writer-wins.</summary>
    public string? UpdatedAt { get; init; }

    /// <summary>Whether the reading was retracted.</summary>
    public bool Deleted { get; init; }
}

[JsonSerializable(typeof(ReadingLine))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class ReadingJsonContext : JsonSerializerContext;
