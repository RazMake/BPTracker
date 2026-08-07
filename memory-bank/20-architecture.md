# 20 - Architecture

## Shape: thin heads over a fat shared core

```
BPTracker.Desktop (WPF)      BPTracker.Mobile (MAUI Android)
         |                              |
         +-------------+----------------+
                       v
             BPTracker.Presentation      shared ViewModels
                       v
             BPTracker.Application       use cases + ports
                       v
             BPTracker.Domain            entities, value objects, rules
                       ^
             BPTracker.Infrastructure    adapters (SQLite, clock)
```

**Dependencies point inward only.** Infrastructure depends on Application (to implement its
ports); nothing depends on Infrastructure except the composition roots in the UI heads.

## The rule that makes everything else work

> Logic lives in the four `net10.0` libraries. The UI heads contain layout and adaptation only.

This is why the 85% coverage gate is achievable: nothing that matters needs a UI, an emulator
or a window to test. If you find yourself writing an `if` in code-behind, it belongs in a
ViewModel.

## Project responsibilities

| Project | Contains | Must never contain |
| --- | --- | --- |
| `Domain` | Entities, value objects, domain rules, trend maths. | Any dependency at all. It has zero package references by design. |
| `Application` | Use cases, port interfaces, DTOs. | Concrete I/O, file access, UI types. |
| `Infrastructure` | Journal storage, storage location, system clock. | Business rules. |
| `Presentation` | ViewModels, validation, view state. | Platform types (`System.Windows`, `Microsoft.Maui`). |
| `Desktop` / `Mobile` | XAML, composition root, platform adapters. | Business rules, validation, calculations. |

Adding a dependency to `Domain`, or a `PackageReference` that crosses these lines, requires an ADR.

## Key design decisions in the code

- **Readings are immutable.** `BloodPressureReading` is a sealed record with `init` properties and
  a private copy constructor, so edits must go through `Create` / `WithContext` / `Retract`.
- **Time is injected** via `IClock`. Nothing calls `DateTime.Now` outside `SystemClock`.
- **Soft deletes.** Retracting sets `IsDeleted` and stamps `UpdatedAtUtc`, so the deletion can sync.
- **Conflict resolution is explicit.** `BloodPressureReading.ResolveConflict` is last-writer-wins,
  with ties resolved in favour of the retraction.
- **Storage is append-only NDJSON, one journal per device.** No database. See
  [80-storage-and-sync.md](80-storage-and-sync.md) and
  [ADR-0002](decisions/ADR-0002-journal-storage.md).

## Sync

The app does not sync. An external tool watches a user-chosen folder and copies files between
devices. Each device writes only its own journal and reads them all, so the sync tool never sees a
conflict. Full detail in [80-storage-and-sync.md](80-storage-and-sync.md).
