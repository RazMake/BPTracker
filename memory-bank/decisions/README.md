# Architecture decision records

One file per decision, numbered, never edited once accepted. To reverse a decision, write a new
ADR that supersedes the old one and add a note at the top of the old one pointing to it.

Write an ADR when a decision **constrains future work**. Examples that require one:

- Adding a dependency to `BPTracker.Domain`.
- Excluding code from coverage measurement.
- Changing the 85% floor or the 400-line file limit.
- Changing how readings are stored, identified or merged.
- Replacing a component listed in [../30-tech-stack.md](../30-tech-stack.md).

Template:

```markdown
# ADR-NNNN - Title

- **Status:** proposed | accepted | superseded by ADR-NNNN
- **Date:** YYYY-MM-DD

## Context
What forced a decision.

## Decision
What was chosen, in one sentence.

## Consequences
What this makes easy, what it makes hard, and what has to be revisited later.

## Alternatives considered
What was rejected and why.
```
