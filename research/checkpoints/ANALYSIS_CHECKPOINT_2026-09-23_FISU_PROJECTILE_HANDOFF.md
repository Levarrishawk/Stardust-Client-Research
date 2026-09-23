# FISU context and projectile handoff (block 1 of 3)

Static target: original `C:/Stardust-DEV/Stardust.exe`. Follows the prior `FISU/0002` parser checkpoint; decompiler types and names remain provisional.

## Confirmed by code

`0x005FE580` enters `0x005FEAD0` before creating fire actions. The resolver first requires an accepted actor slot (`0x00407A70`), obtains a context through `0x004085F0`, checks `0x005F9B00`, and traverses a collection from `0x007E42F0`/`0x007E4370`. It reads names from members of that collection, recognizes a four-character prefix (literal behind `PTR_DAT_015DF384`), splits a trailing underscore-number suffix, and constructs an indexed vector. Because the underlying collection and prefix were not independently identified, calling these definitively muzzle names would be premature.

The nonzero-count route dispatches per element to `0x005FEE40` or `0x005FEFE0`. Both build an action name using format strings `fire_%d` or `fire_%d_%d`, obtain a playback action container through `0x00408460`, construct a `FireProjectileAction`-family object through `0x005FD240`, and append actions through `0x00408610`. `0x005FEE40` also constructs `0x005FA080` with four-character tag `CAHD` (little-endian immediate `0x44484143`). A branch of `0x005FEFE0` constructs the same helper with tag `CAMD` (`0x444D4143`). Thus the previous uncertainty about **whether FISU consumes CAHD/CAMD is resolved positively for these branches**. The source and semantics of the helper's output are not yet fully resolved. `0x005FEFE0` has special cases driven by the third template field (`+0x10`), including values 0, 3, and 4; avoid generalizing all fire types from one case.

## Remaining boundary

This establishes script-action construction and the variable-tag bridge, not the full projectile trajectory, collision, rendering, or server hit authority. Next candidates: `0x005FD240` runtime action, `0x005FA080` tag accessor, `0x005F9B00` context accessor, and `0x007E4370` collection provider. No network packet mapping follows directly from this local playback path.

Provisional identifiers: `0x005FEAD0` `resolve_FISU_fire_context_and_indexed_elements`; `0x005FEE40` `append_FISU_fire_actions_variant_A`; `0x005FEFE0` `append_FISU_fire_actions_variant_B` (variant meaning still unknown).
