---
applyTo: "tests/**/*.cs"
---

# Test authoring rules

Full detail in [memory-bank/50-testing.md](../../memory-bank/50-testing.md).

- **xUnit v3** + **Shouldly** (`ShouldBe`, `ShouldThrow`) + **NSubstitute** for fakes.
  `Xunit` and `Shouldly` are global usings; do not add them per file.
- Use `BPTracker.TestSupport`: `TestClock` for deterministic time, `ReadingFactory` for builders.
  Never call `DateTime.Now` in a test.
- Name the test as a sentence about behaviour: `CreateRejectsSystolicNotAboveDiastolic`.
  Do not use `Method_Scenario_Result` underscores.
- One behaviour per test. Use `[Theory]` with `[InlineData]` for boundary tables, and test the
  edges of a range rather than a value in the middle.
- Always cover the failure paths: argument guards, out-of-range values, and null arguments.
- Repository tests use a real SQLite file via `SqliteDatabaseFixture`, never a fake. Round-trip
  tests must assert the UTC instant **and** the offset.
- Every test needs at least one assertion; `S2699` will fail the build otherwise.
