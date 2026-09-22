# Input MAP Inheritance and Binding Precedence

Date: 2026-09-21

## Runtime selection

`input_map_rebuild_active_bindings_VA_00AADEB0` rebuilds a 32-bit active
shift-state value from the 32 shift-key reference slots, clears the dense
device lookup tables, then walks the loaded MAP linked list until `MAP.INFO`
equals that exact state.

The Boolean byte from `MAP.INFO` has a concrete role. When it is true and a
zero-state map exists, the rebuild applies the zero-state map first, then the
selected map. It is therefore a **base-map inheritance/overlay flag**. When it
is false, only the selected map is applied.

This matches the corpus distribution: ground state 4 maps commonly inherit
base state 0; space nonzero states all inherit base; ordinary state 2/6 ground
maps are complete maps with the flag clear.

## Duplicate precedence

`input_map_apply_sparse_bindings_VA_00AADDC0` iterates each family's sparse
records in serialized order and assigns the command pointer directly into the
dense array at the physical index. A repeated `(family, physicalIndex)` is
therefore **last record wins**.

The only effective duplicate tuple is in
`input/groundinputmap_mmorpg2.iff`, state zero, `KEYS` index 14:

1. `CMD_clearCombatQueue`
2. `CMD_chatBackspace`

`CMD_chatBackspace` is the runtime winner. This is now a regression fixture,
not an unresolved asset anomaly.

Overlay order is similarly explicit: inherited base bindings are applied
first, then selected-state bindings overwrite matching dense slots.

## Replacement contract

1. Recompute exact active state from the 32 shift references.
2. Select the first MAP whose INFO state equals it.
3. If the selected MAP's inheritance flag is true, apply state-zero MAP first.
4. Apply selected MAP records in serialized order into bounds-checked dense
   tables; later rows win.
5. Rebuild joystick button/axis/slider/POV lookup tables under the same order.

## Confidence

**Confirmed** by `0x00AADEB0`, `0x00AADDC0`, loader construction order and the
effective 14-profile corpus.

## Evidence

- `decompiled-input-policy-runtime/00aadeb0_FUN_00aadeb0.c`
- `decompiled-input-policy-runtime/00aaddc0_FUN_00aaddc0.c`
- `input_physical_bindings.csv`
