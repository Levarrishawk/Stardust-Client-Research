# CDF Engine Glow and On/Off Attachment Block

Date: 2026-09-21

## Result

This block reconstructs the original-asset records that drive ship engine glows and state-dependent appearance attachments.

## GLOW record

`FUN_00606250` recognizes repeated `FORM GLOW` records, constructs each through `FUN_006FFCD0`, loads it through `FUN_006FFF10`, and stores it in the `ClientDataFile` collection at `+0x6C`.

### GLOW/INFO layout

1. logical glow name string;
2. hardpoint name string (optional);
3. primary shader-template path string;
4. no-Z/alternate shader-template path string;
5. RGBA/color of four `float32` values;
6. three angular/profile size `float32` parameters: perpendicular/base scale, negative-axis endpoint scale and positive-axis endpoint scale;
7. optional `INF2` position/direction vector (`3 x float32`).

The loader eagerly resolves the two shader-template strings through `FUN_0075AB70` and stores cached handles at record offsets `+0x58` and `+0x5C`.

The effective TIE interceptor CDF contains four records: two ordinary engine shaders and two bloom shaders, attached to `engine_glow1` and `engine_glow2`. The ordinary records use seven floats `1.0, 1.0, 0.5, 0.5, 0.4, 0.7, 0.7`; bloom variants use `1.0, 0.5, 0.4, 0.4, 0.5, 1.25, 2.5`.

### Runtime creation

`FUN_006052D0` is the confirmed ClientDataFile glow-instantiation routine. It iterates the `+0x6C` records and:

- resolves the named hardpoint transform when present;
- invokes `FUN_00700040` to construct the glow appearance from the configured shader data;
- creates either a normal object or specialized particle/appearance object as required;
- names the child with the configured logical name, falling back to the embedded `unnamed_glow` string;
- attaches it to the owning object and validates the final glow setup;
- logs explicit diagnostics on creation failure, missing/invalid hardpoint, or invalid glow configuration.

The routine accepts a mode flag that suppresses or alters creation, providing a state-control boundary for engine/offline transitions.

## ONOF record

`FORM ONOF` is loaded inline by `FUN_00606250` and appended to the collection at `ClientDataFile +0x4C`.

Observed grammar:

1. `INFO`: 32-bit integer interpreted as boolean state selector;
2. `APPR`: appearance-template path string, eagerly resolved;
3. one optional placement source:
   - `PSOR`: explicit position plus three angular `float32` values; or
   - `HARD`: hardpoint name string.

The effective freighter-light record has state selector `1`, appearance `appearance/cargo_freighter_engine_on.apt`, and hardpoint `engine_on1`. This confirms the record represents an appearance activated for a particular binary object/engine state. Whether boolean `1` is generically "on" or specific to a broader condition enum remains strongly inferred rather than fully proven.

## Replacement-client consequence

The renderer must not bake engine glows into meshes. Original assets expect a runtime attachment system that can:

- resolve hardpoints from appearance/skeleton data;
- instantiate shader-driven procedural glow geometry;
- support primary and no-Z shader variants plus bloom-specific records;
- update or suppress attachments as object state changes;
- attach ordinary `.apt` appearances for binary ONOF states;
- destroy and recreate transient child objects without leaking handles.

The glow runtime builds a camera-facing quad. It selects the primary or alternate/no-Z shader according to which side of the glow plane faces the camera, evaluates an additional distance/appearance scale, and piecewise-linearly interpolates quad size from the perpendicular/base scale toward the configured negative- or positive-axis endpoint scale. The RGBA vector is forwarded into the render-primitive base. Preserve all values as original float32 data and expose the three size parameters explicitly; exact historic member spelling remains unavailable.

## Confidence

- Tags, field ordering, widths, shader resolution, and storage: confirmed.
- Glow runtime creation/attachment/error paths: confirmed.
- ONOF binary state attachment purpose: strongly inferred from loader, flag, and effective engine-on asset.
- Four-float RGBA role and three-value angular size profile: confirmed by the runtime primitive constructor/render path; exact historic member spelling is unknown.
