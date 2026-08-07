# ADR-0001 - Thin UI heads over a shared core, with coverage measured on the core

- **Status:** accepted
- **Date:** 2026-08-06

## Context

The repository hosts two clients (WPF desktop, MAUI Android) and the requirement is a minimum of
85% test coverage for both, plus well-modularised, testable code.

Measuring coverage of XAML-driven UI code requires either a UI automation harness (Appium, or
WinAppDriver plus an Android emulator) or elaborate view mocking. Both are slow, flaky, and would
roughly double CI complexity for very little defect-finding value.

## Decision

All behaviour lives in four platform-neutral `net10.0` libraries - `Domain`, `Application`,
`Infrastructure`, `Presentation` - and the coverage gate is measured against exactly those four.
The UI heads contain layout, composition-root wiring, and framework adaptation only.

## Consequences

**Easy:** every meaningful behaviour is unit testable on the host with no emulator; ViewModels are
shared verbatim between WPF and MAUI; CI stays fast.

**Hard:** the guarantee depends on discipline. If logic leaks into code-behind it is invisible to
the gate. This is mitigated by the file-length limit on UI files and by treating "no logic in
code-behind" as a review rule.

**Revisit when:** a defect is traced to untested UI-head code, or the heads stop being trivial.

## Alternatives considered

- **Measure everything including the heads.** Rejected: hitting 85% would require UI automation,
  and the resulting tests would be slow and brittle rather than useful.
- **Drop the coverage requirement for the heads and don't state a rule.** Rejected: without the
  thin-head rule the exclusion is just a loophole.
