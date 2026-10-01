# October 1, 2026 — Authored secondary collision extents

## Question and inputs

Can the replacement distinguish authored APPR collision extents from primary display bounds, and do the pinned room/door appearances provide primitive collision extents? Stop at a bounded decoder and independent original-byte agreement; authored NULL does not identify the selected runtime actor collider.

Executable baseline SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Read-only static loader `00B2CCA0` calls primary loader `00B2CCF0`, then secondary loader `00B2CD10`. Both call extent factory `012707C0`; the first stores template `+0C`, the second stores `+10` and invokes nonnull extent virtual `+40`. Confirmed static observations; original executable not run.

Reuse three Mustafar room meshes, door DTLA root and both leaf meshes. Configuration SHA-256: `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`; 48 descending mounts. Source manifest SHA-256: `A1384E6D8C9CCCC884ABD4B920FE33FD38475E7C12354F10B43E6321E21187F6`. Every priority, byte count and source SHA is checked. Detailed inventories/decompilation remain ignored.

## Implementation and validation

`AppearanceExtentDecoder.DecodeCollisionBase` reads the second APPR/0003 record. Missing secondary data is rejected, distinct from valid empty FORM NULL. Shared `DecodeExtent` supports NULL, EXSP/0001 and EXBX/0001; unknown mesh/composite families, malformed NULL, invalid versions, nonfinite values, negative radius and reversed boxes are rejected. No primary fallback or runtime collision conversion is fabricated.

All six original appearances have primary EXBX/0001 and NULL secondary extents. Independent `verify_authored_collision_extents.py` matches native primary coordinates/radius after F32 normalization and every secondary NULL. Prior pinned POB raw-byte inspection also observes NULL CELL bounds for cells 5/6/7. These are sample statements, not corpus distributions or proof of absent physical collision.

Native synthetic gates cover separate primary/secondary values, NULL versus missing, sphere/box decode, unsupported CMSH and malformed NULL/radius. Full native smoke passes. Godot builds with zero warnings/errors. Link/diff validation follows promotion.

## Consequence and next probe

See the [durable extent contract](../../docs/APPEARANCE_EXTENT_DISTANCE_CONTRACT.md) and [actor participation specification](../../docs/GROUNDED_ACTOR_COLLISION_PARTICIPATION.md). The modern door primary-box policy remains explicit; authored NULL does not imply a primary-box or triangle fallback.

Next: resolve alternate owner extent source and conditional synthesized sphere in `01260E40`, then establish the movement caller's body shape and ordering. Canonical names remain 2,599; research/protocol metrics unchanged.
