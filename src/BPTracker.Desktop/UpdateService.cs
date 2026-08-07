using System.Reflection;
using Velopack;
using Velopack.Sources;

namespace BPTracker.Desktop;

/// <summary>
/// Checks GitHub Releases for a newer build and applies it.
/// </summary>
public static class UpdateService
{
    /// <summary>
    /// Assembly metadata key carrying the releases URL, set in the csproj so the
    /// repository location is configured in one place rather than hardcoded here.
    /// </summary>
    private const string ReleasesUrlKey = "ReleasesUrl";

    /// <summary>
    /// Looks for an update and, if one exists, downloads it and restarts into it.
    /// </summary>
    /// <returns>
    /// The version that was applied, or <see langword="null"/> when the app is up to date or
    /// is running from a development build rather than a Velopack installation.
    /// </returns>
    public static async Task<string?> CheckAndApplyAsync()
    {
        var releasesUrl = GetReleasesUrl();
        if (string.IsNullOrWhiteSpace(releasesUrl))
        {
            return null;
        }

        var manager = new UpdateManager(new GithubSource(releasesUrl, null, prerelease: false));

        // A developer running from `dotnet run` is not a Velopack install; do nothing.
        if (!manager.IsInstalled)
        {
            return null;
        }

        var update = await manager.CheckForUpdatesAsync().ConfigureAwait(false);
        if (update is null)
        {
            return null;
        }

        await manager.DownloadUpdatesAsync(update).ConfigureAwait(false);
        manager.ApplyUpdatesAndRestart(update);

        return update.TargetFullRelease.Version.ToString();
    }

    private static string? GetReleasesUrl() => Assembly
        .GetExecutingAssembly()
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .FirstOrDefault(attribute => attribute.Key == ReleasesUrlKey)
        ?.Value;
}
