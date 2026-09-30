# Floor movement milestone — 2026-09-30

User instruction: work to the next milestone after [the completed overnight run](IMPLEMENTATION_SESSION_2026-09-29_TO_2026-09-30_OVERNIGHT.md). This is subsequent work; it is not part of the overnight deadline report.

## Completed

- Static wrapper/classifier/edge-predicate/caller chain and runtime field expansion documented in the [durable contract](../../docs/SINGLE_CELL_FLOOR_MOVEMENT_CONTRACT.md).
- Owned engine-neutral single-cell movement with explicit boundary, collision-policy, topology and numeric stops.
- Godot two-pick movement inspection, contact line/marker and deterministic reset/disposal.
- Full native smoke suite, 500 seeded grid walks and independent 17,658-probe / 4,441-floor agreement.
- Seven reflected original-cell movement adapters, two-pick controls, headless/GPU host gates and visually inspected capture.

[Checkpoint](../checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_SINGLE_CELL_FLOOR_MOVEMENT.md) contains input pins, corrections, precise limitations and reproduction. Research scores remain 89% behavioral / 94% readiness; 2,590 stable address names and 345 packet rows remain unchanged. No commits or staging.

Workspace validator passes: 765 Markdown files, zero broken links, zero private links, zero duplicate address keys and no public file at least 25 MiB. `git diff --check` passes; existing line-ending warnings are informational. The intentionally dirty tree was inspected and preserved; staging is empty. Final GPU/headless control gates passed after the last host changes.

## Resume

Trace special edge continuation `0x012792B0` / `0x01275FE0`, then owner/candidate selection `0x0126AA80` and entry permission around `CellObject+0x29C`. Establish one paired-portal cell transfer contract before implementing it. The present horizontal walker deliberately stops when a crossable edge has no same-floor neighbor. Original full collision modes and enum names remain unresolved.
