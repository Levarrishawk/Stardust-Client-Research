# ClientDataFile child motion controllers

Date: 2026-09-21

## Result

This block closes the five previously generic motion fields in the fixed 68-byte `CHLD`/`CHL2` payload. MSVC RTTI identifies the exact runtime classes as `RotationDynamics`, `SeesawDynamics`, and `SpringDynamics`.

## Controller selection and fields

The selection order in `ClientDataFile_applyToObject_VA_00604420` remains mutually exclusive.

| Serialized values | Runtime class | Behavior |
|---|---|---|
| vector A | `RotationDynamics` | each component is multiplied by frame delta and applied as continuous local rotation around the three engine axes |
| vector B + scalar B | `SeesawDynamics` | vector direction selects X/Y/Z axis; vector magnitude drives sine phase/frequency; scalar is symmetric angular amplitude |
| vector C + scalar C | `SpringDynamics` | normalized vector is oscillation direction; vector magnitude is displacement amplitude; scalar drives sine frequency; original child translation is equilibrium |

### RotationDynamics

Constructor `0x00B353E0` retains the three-component vector. Alter `0x00B35470` applies `deltaTime * component` through the engine's three rotation functions. Its alternate branch preserves pivot-relative translation when the inherited flag is set.

### SeesawDynamics

Constructor `0x00B4AAB0` retains principal-axis enum, vector magnitude and scalar amplitude plus randomized initial phase. Alter `0x00B4AB70` advances elapsed time and computes a sine wave mapped from `[-amplitude, +amplitude]`, then applies that angle on the selected axis.

### SpringDynamics

Constructor `0x00B4A830` retains equilibrium translation, normalized direction, vector magnitude, scalar frequency and randomized initial phase. Alter `0x00B4A960` computes a sine wave using the scalar and a global angular-frequency constant, transforms the direction by current orientation, scales it by vector magnitude, and offsets the equilibrium translation.

## Replacement-client contract

Decode these fields directly into a tagged motion-controller union rather than retaining anonymous vectors. Preserve file float32 values, apply conversion only at the controller boundary, and retain the original priority rule when malformed content populates multiple controller groups.

## Confidence

- Exact runtime class names: **Confirmed by RTTI**.
- Selection priority, retained fields and update equations: **Confirmed by executable code**.
- Behavioral meanings of vector magnitude and paired scalar: **Confirmed by the sine/rotation/translation equations**.
- Exact original tool/editor field names and unit spelling: **Unknown**.
