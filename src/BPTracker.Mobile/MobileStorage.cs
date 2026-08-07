namespace BPTracker.Mobile;

/// <summary>
/// Resolves the Android paths for settings and for the synced journal folder.
/// </summary>
internal static class MobileStorage
{
    /// <summary>
    /// Per-device settings live in app-private storage. They must never be synced.
    /// </summary>
    internal static string SettingsFolder() => FileSystem.AppDataDirectory;

    /// <summary>
    /// Shared Documents, so the folder is visible in a file manager and can be watched by a
    /// sync app. Falls back to app-private external storage when the platform will not allow it,
    /// which keeps the app usable if the user declines all-files access.
    /// </summary>
    internal static string DefaultDataFolder()
    {
        if (HasAllFilesAccess())
        {
            var documents = Android.OS.Environment
                .GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDocuments)
                ?.AbsolutePath;

            if (!string.IsNullOrWhiteSpace(documents))
            {
                return Path.Combine(documents, "BPTracker");
            }
        }

        // Still reachable over USB and in most file managers, just less convenient.
        return Path.Combine(FileSystem.AppDataDirectory, "BPTracker");
    }

    /// <summary>Whether the app may write outside its own sandbox.</summary>
    internal static bool HasAllFilesAccess() =>
        !OperatingSystem.IsAndroidVersionAtLeast(30) || Android.OS.Environment.IsExternalStorageManager;
}
