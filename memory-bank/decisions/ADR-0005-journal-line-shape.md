# ADR-0005 - The journal line is Date, Time, Sys, Dia, Tag

- **Status:** accepted
- **Date:** 2026-08-08
- **Amends:** [ADR-0002](ADR-0002-journal-storage.md). The storage model is unchanged: still NDJSON,
  still append-only, still one file per device. Only the shape of a line changes.

## Context

ADR-0002 promised a file the user can open in a text editor and understand. The line it produced
was not that:

```json
{"Id":"...","Systolic":128,"Diastolic":82,"MeasuredAt":"2026-05-04T09:15:00.0000000+03:00","Arm":"Left","Position":"Sitting","Note":"after coffee","UpdatedAt":"...","Deleted":false}
```

Half of it is noise. `Arm` and `Position` were never asked for by either app and were always
`Unspecified` in practice. `MeasuredAt` is a machine timestamp where the user thinks in a date and
a time of day. `Note` had already been renamed to `Tag` everywhere except on disk.

## Decision

A line leads with the reading as the user thinks of it, then carries the bookkeeping needed to
merge two devices:

```json
{"Date":"2026-05-04","Time":"09:15","Sys":128,"Dia":82,"Tag":"after coffee","Id":"...","UpdatedAt":"...","Deleted":false}
```

- `Date` is `yyyy-MM-dd` and `Time` is `HH:mm`, both **local wall clock**. A missing or blank
  `Time` reads as **07:30**, because a reading with no time recorded is a morning one.
- `Arm` and `Position` are **dropped**, from the file and from anything written in future.
- `Id`, `UpdatedAt` and `Deleted` **stay**. They are what makes last-writer-wins merging and
  syncable retraction possible, and neither survives being reconstructed from a date and a time.
- The parser reads **both shapes**. On startup a device rewrites **only its own journal** into the
  new shape, via a staging file that is moved into place, so an interrupted migration cannot leave
  a half-written journal. Another device's journal is never touched and is read as it arrives.

## Consequences

**Easy:**
- The file finally reads the way the app talks: a date, a time, two numbers and a tag.
- Nothing to coordinate across devices. Each rewrites its own file when it next starts, and until
  it does, every device can still read it.

**Hard:**
- **Minute resolution.** Seconds are gone. Two readings in the same minute are still distinct
  records because `Id` is the key, but they sort by the same timestamp.
- **The original UTC offset is gone.** A reading taken abroad is written as the wall clock of the
  device that recorded it and read back against the reading device's zone. For a personal tracker
  the wall clock is the fact that matters; the instant is not.
- Migration silently drops lines that never parsed. They never loaded either, so nothing visible
  is lost, but a hand-mangled line will not survive to be hand-repaired.
- `MeasurementArm` and `BodyPosition` still exist in the domain with nothing writing them. They
  should go when something else forces a domain change.

**Revisit when:** seconds or a real offset are needed - for instance if readings ever arrive from
a device that records them itself.

## Alternatives considered

- **Only the five fields, no `Id`/`UpdatedAt`/`Deleted`.** Rejected: Date + Time becomes the key,
  two devices can no longer resolve an edit, and a retraction cannot be written at all.
- **Rewrite every journal in the folder on startup.** Rejected outright: writing another device's
  file is the one thing that makes external sync unsafe.
- **CSV instead of NDJSON.** Rejected: the journal and the CSV export would become the same file,
  and appending a partial line to a CSV is no safer, just less obvious.
