# ADR-0002 - Per-device NDJSON journals instead of a database

- **Status:** accepted
- **Date:** 2026-08-06
- **Supersedes:** the SQLite storage described in the original tech stack notes
- **Amended by:** [ADR-0005](ADR-0005-journal-line-shape.md), which reshapes the line. The storage
  model below is unchanged.

## Context

Sync is handled entirely outside the app: an external tool (OneDrive, Google Drive, Syncthing)
watches a folder and copies files between the phone and the desktop. The app never initiates a
sync and never sees the other device except through files that appear in that folder.

The original design stored readings in SQLite. That is unsafe under file-level sync:

- SQLite state spans `.db`, `.db-wal` and `.db-journal`. A sync tool copies whole files at
  arbitrary moments, so it can capture a torn set and produce a corrupt or silently stale database.
- Two-way sync of a binary file resolves conflicts by overwriting one side. Readings are lost with
  no error and no way to recover them.
- A binary file is not "discoverable" in any useful sense; the user cannot inspect or repair it.

## Decision

Readings are stored as append-only NDJSON journals in a user-visible, user-changeable folder.
Each device writes **only** `readings-{deviceId}.ndjson` and reads **every** journal in the folder,
merging by `BloodPressureReading.ResolveConflict` (last-writer-wins on `UpdatedAtUtc`).

There is no database. The merged set is held in memory and reloaded when any journal's size or
timestamp changes.

## Consequences

**Easy:**
- No two devices ever write the same file, so the sync tool never has a conflict to resolve.
- A file captured mid-write only loses its trailing incomplete line, which the parser skips.
- The format is human-readable, so "discoverable" is literally true: the user can open it in a
  text editor, and could repair or hand-edit it.
- An edit is an append, so history is never destructively rewritten.

**Hard:**
- Journals grow without bound. At a few readings a day this is irrelevant for years, but a
  compaction step will eventually be wanted. Compaction must only ever rewrite the local device's
  own journal.
- The whole history is loaded into memory. Fine at this scale, wrong at a million readings.
- There is no query engine; filtering is LINQ over the in-memory set.

**Revisit when:** journals exceed a few megabytes, or reading the folder becomes noticeably slow.

## Alternatives considered

- **SQLite in the synced folder.** Rejected: corruption and silent data loss, as above.
- **SQLite locally plus a journal for sync.** Rejected: two persistence layers to keep consistent,
  for no benefit at this data scale.
- **One shared journal file written by both devices.** Rejected: that is precisely the write
  conflict the per-device split exists to avoid.
