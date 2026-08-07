# 40 - Coding standards

These are enforced by the build, not by review. A violation is a build error, not a comment.

## The four hard rules

| Rule | Enforced by | Threshold |
| --- | --- | --- |
| No warnings | `TreatWarningsAsErrors` in `Directory.Build.props` | zero |
| No large files | `build/FileLength.targets` | warn 300 lines, **error 400** |
| Coverage floor | ReportGenerator in CI and `dev.ps1 coverage` | **85%** lines |
| Modular design | SonarAnalyzer + Roslynator via `Directory.Packages.props` | see below |

## Modularity rules that actually fire

These have already caught real problems in this codebase, so do not treat them as theoretical:

| Rule | Meaning | What to do instead of suppressing |
| --- | --- | --- |
| `S107` | More than 7 parameters. | Extract a parameter object. This is how `MeasurementContext` was born. |
| `S3776` | Cognitive complexity too high. | Extract methods. An untestable method is a design smell. |
| `S138` | Method too long. | Split it. |
| `S1200` | Type depends on too many other types. | The type is doing too much; split responsibilities. |
| `S1192` | Duplicated string literal. | Promote to a `const`. |
| `S1118` | Static-only class without `static`. | Mark the class `static`. |
| `S1075` | Hardcoded URI. | Move it to configuration or assembly metadata. |

## SOLID, concretely

- **S** - one reason to change per type. The file-length gate is a proxy for this.
- **O** - extend by adding a use case or an adapter, not by adding an `if` to an existing one.
- **L** - implementations of a port must honour the port's documented contract, including nullability.
- **I** - keep ports narrow. `IReadingRepository` exists because the ViewModels need those four
  operations, not because SQLite offers more.
- **D** - Application defines the interface; Infrastructure implements it. Never the reverse.

## DRY, with judgement

Deduplicate **knowledge**, not **text**. Two methods that look alike but change for different
reasons should stay apart. Shared build configuration belongs in `Directory.Build.props` and
`tests/Directory.Build.props`; shared test setup belongs in `BPTracker.TestSupport`.

## Style

- File-scoped namespaces, `var` when the type is obvious, braces always.
- Nullable reference types are on everywhere. Do not silence a nullability warning with `!` unless
  the invariant is genuinely enforced elsewhere; prefer restructuring.
- `private` fields are `_camelCase`; interfaces are `IPascalCase`.
- Public members of `src/` libraries need XML documentation. Tests and UI heads are exempt.
- Comments explain *why*. If a comment restates the next line, delete it.

## Suppressions

A suppression needs a scope and a reason, in that order of preference:

1. Fix the design. Nearly always possible.
2. Suppress in the single `.csproj` that needs it, with a comment saying why.
3. Suppress in `.editorconfig` for a path glob, with a comment saying why.
4. Never blanket-suppress at repository level.

Current suppressions and their justifications:

| Code | Scope | Why |
| --- | --- | --- |
| `NU1701` | `BPTracker.Desktop` only | LiveCharts' WPF backend pulls SkiaSharp/OpenTK packages that still ship .NET Framework assets. They work on `net10.0-windows` via the compatibility shim. |
| `xUnit1051` | `tests/**` | Wants a `CancellationToken` threaded through every awaited call. These are millisecond unit tests with nothing to cancel. Revisit if integration tests are added. |
| `CA1848` | repo | `LoggerMessage` source generation is overkill for a single-user desktop app. |
| `CS1591` | UI heads, tests | They are not a public API. |
