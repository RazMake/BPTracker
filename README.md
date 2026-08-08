# BPTracker

Personal blood pressure tracker. Two clients, one shared .NET 10 core.

- **Android (MAUI)** - capture. Two numbers, one hand, under five seconds.
- **Windows (WPF)** - review. History and a trend chart over time.

## Quick start

```powershell
./dev.ps1 setup      # install the SDK, Android toolchain, emulator and packages
./dev.ps1 gates      # build, test, and the 85% coverage gate
./dev.ps1 desktop    # run the WPF app
./dev.ps1 android    # run the phone app on an emulator (boots one if needed)
```

In VS Code: `Ctrl+Shift+B` builds, and the **Desktop (WPF)** launch profile debugs the app.

## Layout

```
src/
  BPTracker.Domain          entities, value objects, rules       (zero dependencies)
  BPTracker.Application     use cases and ports
  BPTracker.Infrastructure  journal storage, system clock
  BPTracker.Presentation    ViewModels shared by both apps
  BPTracker.Desktop         WPF head
  BPTracker.Mobile          MAUI Android head
tests/                      one suite per library + shared test support
memory-bank/                the durable knowledge; read 00-index.md first
build/                      quality-gate MSBuild targets and coverage settings
```

## Storage and sync

The app does not sync. Both apps read and write NDJSON journals in a folder you choose - by
default `Documents/BPTracker` - and you point OneDrive, Google Drive or Syncthing at it.

Each device only ever writes its own `readings-<device>.ndjson` and reads all of them, so there is
never a conflict for the sync tool to resolve. The files are plain text; you can open them.
Both apps show the path and let you change it.

See [memory-bank/80-storage-and-sync.md](memory-bank/80-storage-and-sync.md).

## The rules the build enforces

| Rule | Limit |
| --- | --- |
| Warnings | zero, `TreatWarningsAsErrors` |
| File length | warn at 300 lines, **fail at 400** |
| Line coverage | **85%** across the four shared libraries |
| Modularity | SonarAnalyzer + Roslynator (parameter counts, complexity, coupling) |

None of these are advisory. See
[memory-bank/40-coding-standards.md](memory-bank/40-coding-standards.md).

## Releases

Every push to `main` publishes. Which app is released is decided from the changed paths: shared
code releases both, app-specific code releases only that app, docs release neither.

Desktop and Android version independently (`desktop-v*` and `android-v*` tags) and the patch
number is bumped automatically. The desktop app updates itself from GitHub Releases via Velopack.

Details in [memory-bank/60-build-and-release.md](memory-bank/60-build-and-release.md).

## Contributing

Read [.github/copilot-instructions.md](.github/copilot-instructions.md) - it applies to humans too.
`./dev.ps1 gates` must pass, and [memory-bank/90-active-context.md](memory-bank/90-active-context.md)
should reflect what you changed.

---

Category labels follow the ACC/AHA 2017 bands and are informational only. This is not a medical
device and gives no medical advice.
