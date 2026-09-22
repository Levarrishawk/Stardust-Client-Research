# Analysis Checkpoint: ObjectController Registry Policy

## Result

The fourth argument to `obj_controller_factory_register_VA_00B0CFC0` is strongly identified as a locally/client-originatable capability annotation, but it is dormant metadata in Stardust.exe rather than an active permission or routing gate.

## Evidence

- Registration stores the type's outbound factory, inbound factory and one byte in three independent ordered maps.
- All 24 controller types accepted C→S by current Stardust are registered with byte one.
- Fourteen checked S→C-only types are registered with zero.
- Bidirectional types are registered with one.
- `obj_controller_policy_lookup_VA_00B0D200` does only a map lookup and returns the byte, or zero for absent type.
- An executable-wide scan of all direct `E8 rel32` calls found no call to `0x00B0D200`.
- An executable-wide scan of all PE-section little-endian absolute VAs found no pointer reference to `0x00B0D200`.

## Confidence

- Storage and lookup mechanics: **Confirmed by code**.
- No active call/data references in this executable: **Confirmed by exhaustive static scans for normal x86 direct calls and absolute pointers**.
- Locally/client-originatable label: **Strongly inferred** from perfect checked directional correlation.
- Original source identifier: **Unknown and non-material**.

## Replacement consequence

Do not recreate this third legacy map as an authority boundary. Use explicit `{direction, innerType}` codec registration and enforce C→S authorization on the Stardust server. The legacy byte can remain catalog metadata for diagnostics. It has no effect on inbound parsing, controller dispatch, packet reliability, or transport forwarding in this executable.
