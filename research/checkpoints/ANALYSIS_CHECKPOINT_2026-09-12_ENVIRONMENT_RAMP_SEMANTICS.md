# Environment lighting-ramp semantic closure — 2026-09-12

## Scope and result

This block closes the consumer semantics of every row in the 256×8 lighting-ramp image named by environment-table column 9. The earlier descriptor work established the storage layout but deliberately did not guess friendly names. Direct call-site tracing through `0x00846D00`, named GroundEnvironment objects, fog publication, ShadowVolume state, and generated `_SKY` texture construction now supplies replacement-relevant meanings for all rows.

The original authoring labels remain absent. The names below are stable behavioral names: they describe exactly what the executable does, without claiming to reproduce unpublished editor terminology.

## Confirmed row map

| Ramp row | Decoded channels | Executable consumer | Stable behavioral meaning | Confidence |
|---:|---|---|---|---|
| 0 | RGB + alpha/scalar | RGB is written to the named `ambient light`; scalar is gated by gradient-sky availability and applied to every runtime object created from ENVM `CELS` records | ambient-light color; generic celestial-object opacity | Confirmed |
| 1 | RGB + alpha/scalar | primary color and intensity fields of named `main light` | main-light diffuse color and intensity | Confirmed |
| 2 | RGB + alpha/scalar | secondary color and intensity fields of the same `main light` object | main-light secondary/specular color and intensity | Confirmed structurally; “specular” is the likely legacy label |
| 3 | RGB + alpha/scalar | color and intensity of named `fill light` | fill-light color and intensity | Confirmed |
| 4 | RGB + alpha/scalar | color and intensity of named `bounce light` | bounce-light color and intensity | Confirmed |
| 5 | RGB + alpha/scalar | RGB becomes a six-face one-pixel fallback cube texture registered under `_SKY`; scalar is propagated to four ENVM celestial appearance objects | fallback solid-sky color; celestial visibility/intensity | Confirmed behavior |
| 6 | RGB + alpha/scalar | RGB is published with fog-enable and fog-density to the world cell; scalar drives stars directly and complementary previous/current sky and cloud appearance blend factors | fog color; sky/cloud/stars transition opacity | Confirmed |
| 7 | RGBA float | passed to the ShadowVolume global RGBA setter `0x0078D940` when shadows are enabled; disabled descriptors contribute zero alpha | shadow-volume color and opacity | Confirmed |

## Evidence chain

### Descriptor layout

`0x008510F0` decodes the eight image rows into these arrays:

- row 0 RGB `+0x002C`, scalar `+0x032C`;
- row 1 RGB `+0x072C`, scalar `+0x0A2C`;
- row 2 RGB `+0x0E2C`, scalar `+0x112C`;
- row 3 RGB `+0x152C`, scalar `+0x182C`;
- row 4 RGB `+0x1C2C`, scalar `+0x1F2C`;
- row 5 RGB `+0x232C`, scalar `+0x262C`;
- shadow-enable byte `+0x2A2C` and fog-enable byte `+0x2A2D`;
- row 6 RGB `+0x2A2E`, scalar `+0x2D30`;
- minimum/maximum fog density `+0x3130/+0x3134`;
- row 7 RGBA float array `+0x3138`;
- independent descriptor wind scale `+0x4164`.

Each array has 256 time samples. `0x00846D00` selects the current sample with GroundEnvironment `+0x84` and linearly interpolates previous/current descriptors by its supplied five-second transition fraction.

### Named light objects

GroundEnvironment construction embeds and assigns exact object names:

- `+0x3C`: `ambient light`;
- `+0x40`: `main light`;
- `+0x48`: `fill light`;
- `+0x50`: `bounce light`.

Rows 0–4 feed those fields in that order. Row 2 writes the second color/intensity bank on the main-light object rather than another light.

### Row 0 scalar: generic-CELS opacity

After testing whether each descriptor supplies the gradient-sky resource returned by `0x00850D60`, `0x00846D00` interpolates row 0 scalar, multiplies it by the interpolated resource-presence gate, enumerates the repeated-CELS runtime collection at GroundEnvironment `+0xAC`, and sends the result through `0x00843CD0` to every member appearance. It is therefore neither ambient-light intensity nor shadow opacity.

### Row 5 RGB: fallback sky cubemap

`0x00846D00` converts row 5 RGB to packed bytes at GroundEnvironment `+0x88..+0x8A`. The sole consumer is the no-source-resource branch of `0x00848940`: it creates six one-by-one RGBA texture faces, fills every face from those three bytes with alpha 255, and registers the result under tag `0x5F534B59` (`_SKY`). The resource-present branch instead derives the cube texture from the descriptor sky asset. “Fallback solid-sky color” is consequently code-confirmed.

### Row 6 RGB and scalar

GroundEnvironment stores row 6 RGB at `+0x5C..+0x68`. `0x008488F0` obtains the world cell and publishes the environment fog triple in one sequence: enable byte `+0x58`, this color, and density `+0x6C`. The RGB channel is fog color.

The row 6 scalar is sent to the `stars` appearance at `+0x28`. Exact constructor strings map `+0x20/+0x24` to `previous sky/current sky`, `+0x2C/+0x30` to `previous/current bottom clouds`, and `+0x34/+0x38` to `previous/current top clouds`. During descriptor transition, previous members receive `1-transitionFraction` and current members receive `transitionFraction`, while stars receive the ramp scalar. This channel controls sky-family visibility/crossfade, not fog density.

### Row 7: shadow-volume RGBA

`0x0078D3D0` is the executable-identifiable ShadowVolume install path and references the `shader/shadowvolume_*` family. Its global setter `0x0078D940` receives the row 7 RGBA blend. A descriptor with shadows disabled contributes zero alpha. This is the projected geometry shadow-volume color/opacity channel, distinct from row 0's per-appearance projected/blob-shadow opacity.

## Replacement requirements

1. Decode the source image with the exact 256×8 row ordering and preserve alpha as an independent scalar where applicable.
2. Sample by the original normalized environment-cycle index; do not replace the ramp with a single per-weather constant.
3. Blend old and new descriptor samples for five seconds on family/weather changes.
4. Preserve the distinction between main-light primary and secondary color/intensity banks.
5. Preserve generic CELS opacity from row 0 separately from row 5 sun/moon opacity and row 7 shadow-volume RGBA.
6. If the sky source is absent, synthesize a solid six-face sky resource from row 5 RGB.
7. Publish fog enable, row 6 color, and separately derived density as one world/environment state.
8. Crossfade previous/current sky and two cloud layers independently from the time-varying stars opacity.

## Remaining uncertainty

- The original editor-facing labels for rows 0–7 are not embedded in this executable or datatable. This is nomenclature only; consumer behavior is closed.
- Row 2 is structurally the main light's secondary color/intensity bank. Calling it “specular” is strongly supported by conventional light layout, but the safe replacement field name is `mainLightSecondary` until an original symbol or tool label is recovered.
- The exact modern physically based interpretation should not be inferred from these legacy values. A compatibility renderer should first reproduce the legacy consumers, then optionally expose a separately calibrated modern-lighting path.
