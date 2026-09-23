# Lightsaber appearance runtime composition and blade transitions

Date: 2026-09-23. Target: `C:/Stardust-DEV/Stardust.exe`.

## Result

The `.lsb` template is instantiated as a **composite appearance**, not a
single mesh. `LightsaberAppearance` constructor `0x0064EFD0` builds the base
appearance, then one per-BLAD appearance/primitive for each authored blade.
It parents both base and blade visibility objects under a common DPVS object,
maintains per-blade current lengths and state arrays, and owns cleanup of
those child appearances (`0x0064DE50`). Thus an engine-native replacement may
use a different primitive but must retain each blade as an independently
updated child with the original base/visibility relationship.

The blade record constructor `0x0064F8C0` maps serialized fields into
`+0x14` length, `+0x18` width, `+0x1C` OPEN value, and `+0x20` CLOS value.
Runtime transition helper `0x0064F3A0` distinguishes closed, open, opening,
and closing states: it increases current length during opening using the
`OPEN` value and decreases length during closing using `CLOS`, clamping to
the authored target length or zero. Per-frame method `0x0064E390` iterates
all blades, updates these lengths/states, refreshes hardpoint transforms when
needed, and marks appearance geometry dirty when transition states change.

`0x0064E5B0` also offers blade-specific hardpoint transforms scaled by
current blade length before falling back to base/child appearance hardpoints.
This matters for effects and attachments positioned at a blade tip; a static
decorative mesh would not preserve the same transform behavior.

## Confidence and limits

- Per-BLAD object creation, shared visibility parent, per-blade transition
  arrays, close/open interpolation, and cleanup: **confirmed by executable
  code**.
- Exact historical enum names and shader primitive tessellation details:
  **partially known**. `OPEN`/`CLOS` behave as transition rates in the traced
  state math; the final physical unit depends on the frame-time caller.
- Authored `LGHT` mapping into an actual render light and `ASND` activation/
  loop behavior remain separate runtime edges to trace. Do not infer a direct
  per-frame sound trigger from the file fields alone.

Evidence: `0x0064EFD0`, `0x0064DE50`, `0x0064F8C0`, `0x0064F3A0`,
`0x0064E390`, `0x0064E5B0`; RTTI vtable `0x015E08A4`. Scratch under
`local-private/session-2026-09-23-marathon/block44_lsat_runtime.txt`.
