# FISU fire-setup action (block 1 of 3)

Scope: original `C:/Stardust-DEV/Stardust.exe` only, correlated with effective `playback/fire_projectiles.pst` and `playback/fire_projectiles_reflect.pst`. Static observations below are not a claim that every combat path was exercised.

## Confirmed by executable code

`0x005FE4A0` installs `FireSetupActionTemplate` through the playback factory registry `0x00404130`; its factory `0x005FE7F0` allocates a 0x1C-byte template and calls `0x005FE850`. That constructor enters `FISU`, accepts version `0002` only, and dispatches to `0x005FE990`. The latter reads exactly five 16-bit values from `INFO`, storing widened values at template offsets `+8,+0xC,+0x10,+0x14,+0x18`. Unsupported versions reach a fatal diagnostic. This corrects a superficial reading of the 10-byte `INFO`: it is not a list of four-character variable tags.

The registered template's method at `0x005FE580` is an action-instantiation/launch route (the exact virtual slot needs a complete class-layout audit). It calls `0x005FEAD0` to resolve fire context, then branches on a count and calls `0x005FEE40` or `0x005FEFE0` per resolved element, with a zero-count path through `0x005FF2F0`. It sets the playback instance stop byte at `+100` on an unsuccessful context path. These calls show a multi-element fire-setup operation, but their downstream projectile/material effects are not yet established; do not equate this to a complete projectile renderer.

## Asset correlation and limits

The effective `fire_projectiles.pst` has a parallel `PSKA` thread and a `FISU/0002` thread. Its ten INFO bytes decode as five little-endian 16-bit values (`0,1,0,0,0` from the previously recorded hex `00000100000000000000`), matching `0x005FE990`'s read widths. The script also defines `CAHD` and `CAMD`, but this block **does not prove FISU consumes either tag**. Context resolver `0x005FEAD0` and downstream `0x005FEE40`/`0x005FEFE0` remain targeted unknowns.

Provisional names: `0x005FE4A0` `install_FISU_FireSetupActionTemplate`; `0x005FE850` `parse_FISU_template`; `0x005FE990` `parse_FISU_0002_INFO`; `0x005FE580` `launch_FISU_fire_setup` (last is strongly inferred, not fully confirmed). All references are raw x86 VAs, not stable API names.
