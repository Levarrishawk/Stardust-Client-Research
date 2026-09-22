# ENVM repeated-CELS record closure — 2026-09-12

## Result

The repeated `CELS` record in `terrain/environment/*.iff` is now structurally and behaviorally closed for replacement use. It describes a camera-relative celestial billboard object with an optional second billboard, fixed yaw/pitch placement, and optional periodic pitch-axis rotation. The loader preserves a 0x34-byte runtime record, but a replacement should store the eight authored fields plus explicit animation state rather than reproduce that layout.

This block also corrects an earlier provisional interpretation: GroundEnvironment `+0xAC` is the collection of runtime objects created from `CELS`, not a shadow collection. Lighting-ramp row 0 scalar controls these generic CELS objects, gated by availability of the current gradient-sky resource. ShadowVolume color remains the separate row 7 path.

## CELS payload schema

`0x008455E0` reads each chunk sequentially with no external padding:

| Order | Wire type | Runtime record offset | Behavioral role | Confidence |
|---:|---|---:|---|---|
| 1 | NUL-terminated string | `+0x00` string object | primary celestial billboard shader | Confirmed |
| 2 | little-endian float32 | `+0x0C` | primary billboard angular/visual size | Confirmed consumer; original unit label absent |
| 3 | NUL-terminated string | `+0x10` string object | optional secondary/glow billboard shader | Confirmed |
| 4 | little-endian float32 | `+0x1C` | secondary billboard angular/visual size | Confirmed consumer; original unit label absent |
| 5 | little-endian float32 degrees | `+0x20` radians | initial local yaw | Confirmed |
| 6 | little-endian float32 degrees | `+0x24` radians | initial local pitch | Confirmed |
| 7 | little-endian float32 | `+0x28` | signed revolutions per animation period | Confirmed by equation |
| 8 | little-endian float32 seconds | `+0x2C` | animation period; values `<= 0` disable animation | Confirmed |

Record `+0x30` is runtime animation phase/cache initialized to zero; it is not a ninth authored field. Payload length varies with the strings. The client does not align either string or the following floats.

## Runtime construction

GroundEnvironment construction iterates the ENVM record vector and calls `0x008439F0` with the primary string/size, secondary string/size, and a false placement flag. That function creates the primary shader-backed billboard only for a nonempty path and positive size, then optionally creates a companion object and second billboard under the same conditions. It retains shared opacity and a companion multiplier used by `0x00843CD0`.

Runtime objects are placed in GroundEnvironment `+0xAC` in CELS chunk order. Per-frame update pairs collection element `n` with record `n`; source ordering is significant.

## Transform and animation equation

For each record, `0x008479B0` starts with identity and applies:

```text
yaw(initialYawRadians)
pitch(initialPitchRadians)
```

The axis functions are mechanically confirmed from their matrix equations: `0x00AB8E10` rotates X/Z (local Y-axis yaw), while `0x00AB8EA0` rotates Y/Z (local X-axis pitch).

When `animationPeriodSeconds > 0`, the client additionally applies:

```text
phaseSeconds = fmod(environmentElapsedSeconds, animationPeriodSeconds)
animationRadians = (phaseSeconds / animationPeriodSeconds)
                   * signedRevolutionsPerPeriod
                   * 2π
pitch(animationRadians)
```

The executable initializes its multiplier at `0x008450C0` as twice pi. Positive and negative scale values select opposite directions. A zero/negative period bypasses the extra rotation even if the scale is nonzero. The final transform is copied into the corresponding runtime object, which is enabled and passed through its camera-relative visibility updater.

## Configured-corpus evidence

The configured overlay contains 63 effective `terrain/environment/*.iff` members and 116 decoded CELS records with no trailing bytes. Only one deployed record has a nonempty secondary shader (`terrain/environment/hutta.iff`), proving that path is real. Yaw values include negative and positive degrees, signed revolution values are -1, 0 and 1, and active period values are 10 and 30 seconds. Exact rows and overlay winners are in `effective_envm_cels.tsv`.

Tatooine's three records decode as:

| Primary shader | Size | Yaw | Pitch | Revolutions | Period |
|---|---:|---:|---:|---:|---:|
| `shader/cels_moon_earthy01.sht` | 0.1 | 0° | 15° | 1 | 0 s (disabled) |
| `shader/cels_star_destroyer.sht` | 0.1 | 0° | 45° | 0 | 10 s (no rotation because scale is zero) |
| `shader/cels_star_destroyer.sht` | 0.3 | 7° | 53° | 0 | 30 s (no rotation because scale is zero) |

## Lighting-ramp relationship correction

`0x00846D00` enumerates the same CELS collection and applies lighting-ramp row 0 scalar through `0x00843CD0`. The scalar is multiplied by a presence gate derived from environment-descriptor `+0x10`, the loaded gradient-sky texture from table column 2. Thus row 0 is ambient-light RGB plus generic CELS visibility/opacity. Row 5 scalar independently drives SUN/SSUN/MOON/SMOO objects; row 7 RGBA independently drives ShadowVolume global color.

## Replacement contract

1. Parse two strings and six float32 fields sequentially and unaligned.
2. Preserve record order.
3. Convert authored yaw/pitch degrees to radians once at load time.
4. Treat field 7 as signed revolutions per field-8 period using the environment elapsed clock and 2π.
5. Create the second billboard only for a nonempty path and positive size.
6. Keep generic CELS opacity, principal sun/moon opacity, and shadow-volume RGBA distinct.
7. Use camera-relative celestial placement/visibility, not world-positioned scene geometry.

## Remaining uncertainty

- Original editor labels and the precise authoring unit name for the size floats are absent; use as billboard-construction size parameters is confirmed.
- `0x00843D50` includes camera-facing visibility/occlusion fading beyond record parsing. Its principal effect is known; exact collision policy belongs to a later fidelity block.
- A handful of effective ENVM files contain nested/custom chunks (`FORM`, `TLOK`, `SKYB`) beyond the base v0000 set. Those require a separate compatibility audit but do not change CELS schema.
