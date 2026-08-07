# BPTracker

Personal blood pressure tracker: an Android app for fast entry, a WPF desktop app for review,
over a shared .NET 10 core.

## Before you change anything

Read [memory-bank/00-index.md](../memory-bank/00-index.md) and open the file that matches your
task. The memory bank is the source of truth for architecture, standards and process.

The two you will need most often:

- [memory-bank/40-coding-standards.md](../memory-bank/40-coding-standards.md) - the rules the build enforces.
- [memory-bank/20-architecture.md](../memory-bank/20-architecture.md) - which project code belongs in.

## Non-negotiables

1. **Zero warnings.** `TreatWarningsAsErrors` is on. Fix the cause; do not suppress. If a
   suppression is genuinely correct, scope it to one project or path glob and write the reason
   next to it.
2. **Files stay under 400 lines** (warning at 300). The build fails past the limit.
3. **Coverage stays at or above 85%** on the four shared libraries.
4. **Dependencies point inward.** `Domain` has zero dependencies. `Application` defines ports;
   `Infrastructure` implements them. UI heads may reference anything; nothing references UI heads.
5. **No logic in UI heads.** Behaviour goes in `Presentation` or deeper, where it is tested.
   This is what makes the coverage number meaningful.
6. **Pulse is not tracked.** Do not add it.
7. **Never log reading values.** They are health data.
8. **A device only ever writes its own journal file.** Never write, rewrite or compact another
   device's journal - that is what makes external file sync safe. See
   [memory-bank/80-storage-and-sync.md](../memory-bank/80-storage-and-sync.md).

## When an analyzer complains

Prefer fixing the design. These rules exist to enforce the SOLID requirement and they routinely
find real problems:

- `S107` too many parameters -> extract a parameter object.
- `S3776` / `S138` too complex or too long -> extract methods.
- `S1200` too many dependencies -> the type does too much.

## Definition of done

- [ ] `./dev.ps1 gates` passes (build with warnings as errors, tests, 85% coverage).
- [ ] New behaviour has tests, including failure paths and argument guards.
- [ ] Public members of `src/` libraries have XML docs.
- [ ] The memory bank is updated: [90-active-context.md](../memory-bank/90-active-context.md)
      always, plus an ADR if the change constrains future work.

## Useful commands

```powershell
./dev.ps1 setup      # first-time setup
./dev.ps1 gates      # everything CI enforces
./dev.ps1 coverage   # coverage report, opens in a browser
./dev.ps1 desktop    # run the WPF app
```
