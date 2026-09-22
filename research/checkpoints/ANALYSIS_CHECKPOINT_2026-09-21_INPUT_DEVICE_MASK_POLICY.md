# Input Device-Family Mask Policy

Date: 2026-09-21

## Corrected field identity

The U32 at command object `+0x18` is an allowed **device-family mask**, not an
allowed shift-state mask. `input_map_sparse_map_load_VA_00AB12F0` reads MAP
`INFO` state into sparse-map `+0x04`, then invokes six family loaders with
independent global constants:

| Binding families | Loader | Required command mask bit |
|---|---:|---:|
| `KEYS`, `MOSB`, `JOYB` | `0x00AB2130` | `0x1` |
| `POV ` | `0x00AB2580` | `0x2` |
| `JOYX` | `0x00AB22A0` | `0x4` |
| `JOYS` | `0x00AB2410` | `0x8` |

The initial executable values at `DAT_015EAAB4..DAT_015EAAC0` are exactly
1, 2, 4, and 8. Each loader resolves the command pointer and warns when
`command.mask & requiredFamilyBit` is zero. MAP `INFO` is used only as the map's
shift-state key and in diagnostics at this boundary.

This also explains the effective command corpus: ordinary ground commands use
mask 3 (key-like plus POV); specialized space/UI commands with masks 4 or 8 are
axis- or slider-only.

## Replacement consequence

- Represent modifier/shift state and accepted device families as different
  types.
- Validate each resolved binding against its family bit.
- Preserve the serialized U32 even if modern devices are normalized behind an
  adapter.
- Treat a mismatch as a diagnostic asset error, matching the legacy loader;
  never reinterpret the number as a modifier state.

## Confidence

**Confirmed** by the loader call arguments, initial global constants, command
object field access, diagnostics, and complete asset-corpus distribution.

## Evidence

- `decompiled-input-policy/00ab12f0_FUN_00ab12f0.c`
- `decompiled-input-policy/00ab2130_FUN_00ab2130.c`
- corresponding axis/slider/POV loaders
- `input_command_actions.csv`
- `input_physical_bindings.csv`
