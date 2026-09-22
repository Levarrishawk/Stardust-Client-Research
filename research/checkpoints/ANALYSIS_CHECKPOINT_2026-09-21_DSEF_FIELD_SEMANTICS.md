# DSEF scalar and spawned-object field closure

Date: 2026-09-21  
Target: `C:\Stardust-DEV\Stardust.exe`  
Boundary: `ClientDataFile` `DSEF` records and `ShipObjectDestruction`

## Result

This block closes the material field-semantic questions deliberately left by the earlier DSEF grammar and runtime passes. The conclusions below come from the loader, post-load resolvers, every accessor cross-reference, the `ShipObjectDestruction` constructor/update paths, and the effective CDF corpus. They are reconstructed contracts, not recovered source declarations.

## Top-level `INFO` scalar

`0x006FD590` returns the float at DSEF `+0x00`. `ShipObjectDestruction` reads it once while constructing its split-plane work records. For successive eligible planes the constructor supplies:

```text
countdown[0] = clamp(0.5   * scalar, 0.1, 20.0)
countdown[1] = clamp(0.25  * scalar, 0.1, 20.0)
countdown[2] = clamp(0.125 * scalar, 0.1, 20.0)
...
```

`0x00613890` stores each value in the split work record at `+0x10` and `+0x14`. `0x00614310` subtracts frame time from `+0x10` and performs the split when it expires. The field is therefore an **initial split-delay seed in seconds**, geometrically halved per seeded split plane and clamped to 0.1–20 seconds. It is not the complete destruction duration.

## Effect-pair floats

The two top-level effect pairs have identical `string + float32` layout but different event roles:

- DSEF `+0x04`: split/break effect and its nominal reference size.
- DSEF `+0x18`: terminal/final effect and its nominal reference size.

For the break effect, `0x00613CE0` calculates uniform effect scale as the current split extent divided by the configured float. For the terminal effect, `0x006155B0` calculates scale as the combined destruction bounds' diagonal length divided by the configured float. A non-positive configured float leaves scale at `1.0`.

The effective shuttle values of `50.0` are therefore **reference lengths for geometry-normalized effect scaling**, not delays or weights.

Every `SXPL/SEDF` record has the same prepared client-effect-plus-reference-length representation. The collection remains part of the prepared destruction data, although this optimized executable does not expose a standalone accessor for it in the traced runtime family. Preserve it as a repeated effect/reference-length record.

## Ambient sound float

Each `ASNL/SDAS` record is prepared as a sound-template handle plus its float. The constructor instantiates one sound object per record. During updates, the float multiplies the distance/radius coupling term used to place that sound along the ship-to-camera axis. The deployed values `1.0`, `1.0`, and `0.5` therefore control **spatial placement/coupling**, not playback volume. The executable equation is replacement-relevant even if the original editor label is unavailable.

## `SDOE` string fields

The complete 44-byte spawned-object record is now identified:

| Field | Meaning | Evidence |
|---|---|---|
| first string (`+0x00`) | object-template name | post-load resolver obtains an object template; runtime invokes its object-creation virtual method |
| second string (`+0x0C`) | appearance-template fallback | used only when the object-template field is empty; runtime creates a plain object and assigns an appearance from this template |
| third string (`+0x18`) | parent hardpoint name | runtime requests this hardpoint transform from the source object's appearance before attaching the spawned object |
| prepared `+0x24` | object-template handle | selected before the appearance fallback |
| prepared `+0x28` | appearance-template handle | fallback construction path |

The sole effective occurrence—empty object template, `appearance/star_destroyer_baffles.apt`, empty hardpoint—therefore creates a plain object with that appearance and uses the default/root attachment transform.

## Replacement contract

The replacement asset IR can now use typed names rather than anonymous DSEF offsets:

```text
initialSplitDelaySeconds
breakEffect { resource, referenceLength }
finalEffect { resource, referenceLength }
splitEffects[] { resource, referenceLength }
ambientSounds[] { resource, spatialCouplingScale }
spawnedObjects[] { objectTemplate, appearanceFallback, parentHardpoint }
```

All serialized values remain 32-bit little-endian floats and NUL-terminated strings. Internal timers, handles, vectors and ownership must be native 64-bit structures.

## Confidence

- Initial split-delay behavior and units: **Confirmed by constructor writes and per-frame countdown consumption**.
- Top-level effect reference lengths: **Confirmed by both split and terminal scale equations**.
- SDAS spatial coupling rather than volume: **Confirmed by transform math and absence from the audio gain setter path**.
- All three SDOE strings: **Confirmed by distinct post-load resolvers and runtime creation/attachment branches**.
- Exact original editor-facing names: **Unknown and not required for behavioral compatibility**.

