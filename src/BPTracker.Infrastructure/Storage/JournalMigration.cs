using BPTracker.Domain.Readings;

namespace BPTracker.Infrastructure.Storage;

/// <summary>
/// Rewrites this device's own journal into the current line shape.
/// </summary>
/// <remarks>
/// This is the one place that replaces a journal instead of appending to it, and it is safe only
/// because it touches the file this device writes and no other. A journal belonging to another
/// device is left exactly as it is; the parser understands both shapes, so an unmigrated one still
/// loads. See <c>memory-bank/80-storage-and-sync.md</c>.
/// </remarks>
internal static class JournalMigration
{
    /// <summary>
    /// Rewrites <paramref name="path"/> if any of its lines are in an older shape.
    /// </summary>
    internal static async Task RunAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return;
        }

        string[] lines;
        try
        {
            lines = await File.ReadAllLinesAsync(path, cancellationToken).ConfigureAwait(false);
        }
        catch (IOException)
        {
            // The sync tool may hold the file. Leave it alone; the next load tries again.
            return;
        }

        if (!TryRewrite(lines, out var rewritten))
        {
            return;
        }

        try
        {
            // Written beside the journal and moved into place, so an interrupted migration leaves
            // the original intact rather than a half-written journal.
            var staging = path + ".migrating";
            await File.WriteAllLinesAsync(staging, rewritten, cancellationToken).ConfigureAwait(false);
            File.Move(staging, path, overwrite: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Readable in the old shape either way, so a failed migration is not worth crashing over.
        }
    }

    private static bool TryRewrite(string[] lines, out List<string> rewritten)
    {
        rewritten = new List<string>(lines.Length);
        var changed = false;

        foreach (var line in lines)
        {
            if (!ReadingLineSerializer.TryParse(line, out var reading))
            {
                // Blank or corrupt. Dropping it loses nothing: it never loaded in the first place.
                changed |= !string.IsNullOrWhiteSpace(line);
                continue;
            }

            var current = ReadingLineSerializer.ToLine(reading);
            changed |= current != line;
            rewritten.Add(current);
        }

        return changed;
    }
}
