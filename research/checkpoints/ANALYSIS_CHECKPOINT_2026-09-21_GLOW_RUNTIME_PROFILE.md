# Glow runtime profile and shader selection

Date: 2026-09-21

## Result

This block follows `ClientDataFile::GlowData` beyond parsing into the concrete runtime primitive. It closes the functional role of the seven `GLOW/INFO` floats without inventing original source names.

## Call chain

```text
ClientDataFile_createGlowObjects_VA_006052D0
  -> GlowData_createAppearance_VA_00700040
     -> GlowAppearance_ctor_VA_007137C0
        -> GlowQuadPrimitive_ctor_VA_00713260
        -> GlowAppearance_finalize_VA_00713C00
```

`0x00700040` passes the cached primary and alternate shader templates, the four-float vector at record `+0x30`, and scalar fields at `+0x40/+0x44/+0x48` into the 0x58-byte quad primitive.

## Recovered parameter semantics

The four-float value is forwarded through the render-primitive base's color/material state and is therefore the glow RGBA/color value.

The three scalars are copied to primitive offsets `+0x38`, `+0x3C`, and `+0x40`. The render path `0x007134C0` computes camera direction in glow-local coordinates and uses the signed side component to:

1. select primary versus alternate/no-Z shader;
2. interpolate from scalar 1 at the plane/perpendicular point toward scalar 2 on the negative side;
3. interpolate from scalar 1 toward scalar 3 on the positive side;
4. multiply that profile by an additional view/distance scale;
5. generate a four-vertex camera-facing quad of the resulting half-size.

The constructor initializes the current size from scalar 3 and caches `max(scalar2, scalar3)` for bounds/culling. This independently confirms these are size/profile values rather than color channels, timing values or alpha thresholds.

Provisional behavioral labels:

| File field | Behavioral label | Confidence |
|---|---|---|
| vector4 | RGBA/color | Confirmed |
| scalar 1 | perpendicular/base size scale | Confirmed functional role |
| scalar 2 | negative-axis endpoint size scale | Confirmed functional role |
| scalar 3 | positive-axis endpoint size scale | Confirmed functional role |

The axis sign is relative to the glow primitive's local orientation. Historic source spelling and whether designers called these front/back rather than positive/negative remain unknown.

## Optional INF2

The optional three-float `INF2` value is retained at record `+0x4C..+0x54`. In the no-hardpoint creation branch, the caller uses its X/Z components to establish a local orientation angle before finalizing the child object. This constrains it to an orientation/direction vector rather than another color or size value. Exact coordinate convention remains open.

## Replacement-client contract

A compatible renderer should create a transient glow attachment that:

- resolves the configured hardpoint or uses the optional direction vector;
- retains both shader variants;
- selects the appropriate shader by camera-facing side;
- evaluates the asymmetric three-point angular size profile;
- applies RGBA data and distance/view scaling;
- emits a camera-facing quad with conservative bounds based on the endpoint maximum.

This behavior maps naturally to a small engine-neutral billboard component and does not require retaining the original D3D9 primitive implementation.

## Confidence

- Call chain, parameter offsets, shader selection, interpolation equation and quad generation: **Confirmed by executable code**.
- RGBA and three-point angular size-profile roles: **Confirmed behaviorally**.
- INF2 direction/orientation role: **Strongly inferred from its no-hardpoint consumer**.
- Exact historic names and axis labels: **Unknown**.
