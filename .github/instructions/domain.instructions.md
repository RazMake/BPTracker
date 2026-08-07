---
applyTo: "src/BPTracker.Domain/**/*.cs"
---

# Domain layer rules

The innermost layer. See [memory-bank/70-domain-glossary.md](../../memory-bank/70-domain-glossary.md)
for terminology and the category bands.

- **Zero dependencies.** Adding any `PackageReference` or `ProjectReference` here requires an ADR.
- No I/O, no `DateTime.Now`, no randomness that is not passed in. Callers supply the current time.
- Entities are immutable: sealed records with `init` properties, mutated through `With*` methods
  that stamp `UpdatedAtUtc`.
- Validate in factory methods. Provide both `From` (throws) and `TryFrom` (does not) for value
  objects, because the entry screens validate on every keystroke and must not throw.
- Deletion is a soft delete (`Retract`), so the tombstone can be synced.
- Classification order in `BloodPressureClassifier` is load-bearing; the bands overlap and the
  most severe match wins. Do not reorder without updating the tests deliberately.
- Pulse is not part of the domain.
