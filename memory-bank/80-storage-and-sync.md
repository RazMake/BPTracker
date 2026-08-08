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

```json
{"Date":"2026-05-04","Time":"09:15","Sys":128,"Dia":82,"Tag":"after coffee","Id":"...","UpdatedAt":"...","Deleted":false}
```

## Robustness rules

- A line that will not parse is **skipped, never thrown**. A file copied mid-sync ends with an
  incomplete line, so this is expected, not exceptional.
- Implausible values are rejected at parse time, so a hand-edited file cannot inject a bad reading.
- A blank `Time` reads as **07:30**, rather than failing the line.
- An over-length tag is **clamped, not rejected**. The limit shrank from 500 to 100 when the note
  became a tag, and an old journal must still load.
- A journal locked by the sync tool is skipped; the next read picks it up.

## Field names on disk

`Date`, `Time`, `Sys`, `Dia`, `Tag`, then `Id`, `UpdatedAt`, `Deleted`. The first five are the
reading; the last three are what makes merging and retraction work. `Arm` and `Position` are no
longer written. See [ADR-0005](decisions/ADR-0005-journal-line-shape.md) for why, and for what is
given up: seconds, and the original UTC offset.

`ReadingLineSerializer` also reads the older shape (`Systolic`/`Diastolic`/`MeasuredAt`/`Note`), so
a folder that has not been fully migrated still loads.

## Migration

`JournalMigration` is the only code that replaces a journal instead of appending to it, and it runs
against **this device's own file only**. It stages the rewrite beside the journal and moves it into
place, so an interrupted migration leaves the original intact. Another device's journal is read in
whatever shape it arrives in and never rewritten.

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

Not implemented. When it is: a device may compact **only its own journal**, never another's - the
same rule the format migration follows.
