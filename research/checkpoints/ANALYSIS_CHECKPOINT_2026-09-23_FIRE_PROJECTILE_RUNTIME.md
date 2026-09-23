# FISU-generated fire-projectile runtime (overnight block 1)

Target: original `C:/Stardust-DEV/Stardust.exe`, static decompilation only. Extends `ANALYSIS_CHECKPOINT_2026-09-23_FISU_PROJECTILE_HANDOFF.md`.

## Confirmed code path

FISU's branches construct a 0x34-byte action through `0x005FD240`. This stores actor/tag/variant fields and installs runtime vtable `0x015DF358`. Its update method is `0x005FD3B0`; the function gates on posture/scene conditions, then resolves source and target points (`0x005FD8A0`, `0x005FD8F0`), creates a projectile object (`0x005FDBC0`), establishes an oriented transform from the two points (`0x005FDC80`), configures speed/lifetime (`0x005FDE40`), conditionally attaches or transfers the projectile (`0x005FDF20`), and invokes an additional weapon-template effect path (`0x005FDF50`). It may create a separate effect object when a boolean variable lookup succeeds. This is a client presentation/action path, not evidence that the client authoritatively determines a hit.

`0x005FDBC0` dynamically identifies `ClientWeaponObjectTemplate` and creates a projectile variant through `0x0060F830`. Its variant switch reads action field `+0x2C` (values 0–4 explicitly handled). `0x005FDE40` reads a float variable by tag at action `+0x14`; it calls the already identified `Projectile_setSpeed` `0x006F5D00`, computes Euclidean source-to-target distance, and sets lifetime through `0x006F5CF0` as distance/speed, except a near-zero-distance fallback to the float constant `0.1`. These are strong, concrete behavioral constraints for a future visual simulation, while exact trajectory, impact, and error handling still require study.

`0x005FE000` selects an object according to the action variant: some modes use a playback actor slot, others query `CreatureController`, and another branch can use a creature-held object. This explains why a replacement should retain **logical actor/weapon references** in playback state rather than bake in one object pointer. The x86 pointer offsets are implementation details, not wire fields.

## Open

The exact meanings of all five variant numbers, `0x005FD8A0`/`0x005FD8F0` hardpoint resolution, collision/impact presentation, and ownership of the spawned projectile. The decompiler's `0x005FD3B0` return expression is partly mangled around floating-point comparisons, so exact continuation/termination timing should be validated from disassembly before being specified.

Provisional names: `0x005FD240` `construct_fire_projectile_action`; `0x005FD3B0` `update_fire_projectile_action`; `0x005FDBC0` `create_fire_projectile_variant`; `0x005FDC80` `orient_projectile_from_source_target`; `0x005FDE40` `set_projectile_speed_and_distance_lifetime`; `0x005FE000` `select_fire_projectile_object_by_variant`.
