# 80 - Storage and sync

## Model

The app does **not** sync. An external tool (OneDrive, Google Drive, Syncthing) watches a folder
and copies files between devices. The app only ever reads and writes files in that folder.

```
Documents/BPTracker/
    readings-019fda8c.ndjson     <- desktop writes this, phone only reads it
    readings-7c41ab02.ndjson     <- phone writes this, desktop only reads it
```

**No device ever writes a file another device writes.** That single rule is what makes external
sync safe: there is never a conflict for the sync tool to resolve. See
[ADR-0002](decisions/ADR-0002-journal-storage.md).

## Format

NDJSON - one JSON object per line, append-only. An edit or a retraction appends a new line for the
same id; the highest `UpdatedAt` wins when the folder is loaded.

Readable on purpose. The user can open it in a text editor.

## Robustness rules

- A line that will not parse is **skipped, never thrown**. A file copied mid-sync ends with an
  incomplete line, so this is expected, not exceptional.
- Implausible values are rejected at parse time, so a hand-edited file cannot inject a bad reading.
- Unknown enum values fall back to `Unspecified` rather than failing the line.
- A journal locked by the sync tool is skipped; the next read picks it up.

## Where the folder lives

| | Default | Settings (never synced) |
| --- | --- | --- |
| Windows | `%USERPROFILE%\Documents\BPTracker` | `%LOCALAPPDATA%\BPTracker\storage.json` |
| Android | `/storage/emulated/0/Documents/BPTracker` | app-private `FileSystem.AppDataDirectory` |

Both apps **show** the folder and the file this device writes, and both let the user **change** it,
because the whole point is to aim it at whatever folder their sync tool watches.

`storage.json` holds the device id and the folder override. It must stay out of the synced folder:
it is per-device state and would be wrong on another machine.

## Android caveat

On Android 11+ scoped storage, `Documents` is not writable without all-files access.

- The manifest declares `MANAGE_EXTERNAL_STORAGE`, and the settings page offers a button to grant it.
- If it is not granted, the default folder falls back to app-private external storage, which still
  works and is still visible over USB, just less convenient.
- `StorageLocation` never throws when it cannot create the folder; it records `Problem`, which the
  settings screen displays. A permission problem must not crash the app on launch.

## Compaction

Not implemented. When it is: a device may compact **only its own journal**, never another's.
