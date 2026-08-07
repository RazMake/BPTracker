# 50 - Testing

## The gate

**85% line coverage**, measured across the four shared libraries only:

```
[BPTracker.Domain]  [BPTracker.Application]  [BPTracker.Infrastructure]  [BPTracker.Presentation]
```

Configured in [build/coverage.runsettings](../build/coverage.runsettings). Enforced by
ReportGenerator's `--minimumCoverageThresholds:lineCoverage=85`, which exits non-zero.

> The `--` prefix matters. With a single `-` ReportGenerator silently ignores the setting and
> the gate passes regardless of coverage. This has already bitten once.

## Why UI heads are excluded

`BPTracker.Desktop` and `BPTracker.Mobile` are not measured. This is only honest because they are
kept logic-free (see [20-architecture.md](20-architecture.md)). The exclusion is a commitment:
if logic starts appearing in a UI head, the gate stops being meaningful. Guard it by moving the
logic into `Presentation` and testing it there.

## Exclusions are the loophole

An 85% gate is trivially defeated with `[ExcludeFromCodeCoverage]`. Therefore:

- Excluding a type or assembly from coverage **requires an ADR**.
- Generated code (`*.g.cs`, `*.Designer.cs`) is already excluded and needs no justification.

## Conventions

- **xUnit v3** plus **Shouldly** for assertions and **NSubstitute** for fakes.
- Test projects are executables (xUnit v3 requires `OutputType=Exe`). This is set once in
  `tests/Directory.Build.props`, so an individual test csproj is just a `ProjectReference`.
- Shared helpers live in `BPTracker.TestSupport`: `TestClock` (deterministic time) and
  `ReadingFactory` (builders). Use them rather than repeating setup.
- Name tests as a sentence describing behaviour: `CreateRejectsSystolicNotAboveDiastolic`.
- One behaviour per test. Use `[Theory]` for boundary tables.

## What to test

- Every domain rule, at its boundaries. The classifier bands overlap, so test the edges.
- Every use case, including the failure path and the argument guards.
- Every ViewModel: validation, command `CanExecute`, and change notification for derived state.
- Repositories against a **real SQLite file**, not a stand-in. `SqliteDatabaseFixture` gives each
  test its own temporary database. Round-trip tests must assert the UTC instant *and* the offset.

## Running

```powershell
./dev.ps1 test        # tests only
./dev.ps1 coverage    # tests + HTML report + 85% gate, opens the report
./dev.ps1 gates       # everything CI runs
```
