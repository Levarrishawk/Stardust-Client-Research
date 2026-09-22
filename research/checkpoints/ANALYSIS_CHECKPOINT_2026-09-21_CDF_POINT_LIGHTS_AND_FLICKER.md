# CDF `LOBJ`/`HLOB`: point lights and point-flicker lights

Scope: resolve the thirteen previously neutral light floats by tracing `0x006095D0` into the original `Light` runtime and comparing its layout with the executable's standalone `POIN` and `FLIC` light loaders.

## Result

The CDF light type field selects:

- `1`: static point light;
- `2`: point-flicker light.

Both can optionally create a referenced appearance (commonly a particle effect) alongside the actual light. `LOBJ` places the light at an explicit object-local vector; `HLOB` binds it to a named hardpoint.

## Common base payload

The parser at `0x00609470` reads:

```text
uint32 type
NUL-terminated optional appearance-template path
float32 value[13]
uint32 runtime_visibility_flag
```

It also supplies implicit alpha `1.0` for each of two RGB color slots. The meaning of `value[]` depends on `type`.

## Type 1: static point light

The live-light constructor consumes:

| Record field | Meaning |
|---|---|
| `value[0..2]` | RGB color |
| `value[6]` | range/radius |
| `value[10..12]` | local position |

The second color triplet and the other point-flicker-only slots are ignored for type 1. The runtime computes luminance from the absolute RGB components, derives the packed/render color, and updates the light's DPVS/spatial range from `value[6]`.

Corpus examples support the mapping: colored street lamps use RGB values such as `(0.102, 0.232, 0.254)`, range `8`, and asset-specific local Y offsets; strong gem lights use range `250`.

## Type 2: point-flicker light

The live-light constructor consumes:

| Record field | Meaning |
|---|---|
| `value[0..2]` | endpoint RGB color A |
| `value[3..5]` | endpoint RGB color B |
| `value[6]` | minimum range |
| `value[7]` | maximum range |
| `value[8]` | minimum update interval |
| `value[9]` | maximum update interval |
| `value[10..12]` | local position |

`Light_alter_VA_006E2EA0` waits for a randomly selected interval in the authored interval range. At each update it independently:

1. selects a random interpolation between the two endpoint colors;
2. applies that color and recomputes luminance/render color;
3. selects a random spatial range between the authored minimum and maximum;
4. selects the next random update interval.

The standalone light loader at `0x006E3100` contains literal diagnostic/type strings `point` and `point_flicker` and writes the same runtime fields, independently confirming the CDF mapping.

## Placement variants

- `LOBJ` appends an explicit local translation vector after the common payload. Application creates the light, applies that translation, optionally creates its companion appearance, and attaches both to the owner.
- `HLOB` appends a hardpoint selector rather than using the `LOBJ` local vector. Application resolves the owner hardpoint transform and places the light/companion presentation there.

The two placement formats must not be normalized at the binary-parser layer because their trailing payloads differ.

## Corpus

A precedence-correct scan found **121 LOBJ/HLOB records in 117 effective CDF files**. Static point lights dominate. Type-2 examples include campfires, cauldrons, braziers, and hologram bases, consistent with randomized color/range flicker.

## Day/night-controlled flag

The final 32-bit boolean is copied to live `Light +0x161`. When set, the light's alter path mirrors its active state to a global value updated once per ground-scene frame from the terrain/world environment (`0x00B5CBF0` virtual `+0x140`, forwarded to `0x006E2DD0`). When clear, the light remains independently active.

The corpus resolves the behavior: each ordinary streetlamp CDF sets the flag, while its explicitly suffixed `_on.cdf` counterpart clears it. Therefore the flag means **follow the environment's night-light enable state**; the `_on` variants are forced-on presentation assets. The exact historic member name is unavailable, so `dayNightControlled` is the stable behavioral label.

## Replacement-client contract

- Preserve the type-discriminated 13-float stream exactly.
- Type 1: RGB + fixed range + position.
- Type 2: two RGB endpoints + random range bounds + random update-interval bounds + position.
- Use independent random samples for color interpolation, range, and next interval as the legacy update does.
- Preserve optional companion appearance creation and the LOBJ-vs-HLOB placement distinction.
- When `dayNightControlled` is set, mirror light activation to the terrain/world night-light state; otherwise leave activation independent.
- A modern renderer can translate these to ordinary point lights; no legacy `Light` memory layout is required.

## Stable provisional names

| Address | Name | Confidence |
|---:|---|---|
| `0x006095D0` | `ClientDataLightRecord_createLight_VA` | Confirmed |
| `0x006E2DE0` | `PointFlickerLight_ctor_VA` | Strongly inferred from class behavior |
| `0x006E2EA0` | `PointFlickerLight_alter_VA` | Confirmed by code |

## Confidence

- Type IDs, static-point mapping, point-flicker mapping, and update equations: **confirmed by code and diagnostic strings**.
- Placement distinction: **confirmed by parser/application code**.
- Day/night-controlled behavior of the final flag: **confirmed by code plus paired streetlamp assets**; exact historic spelling is unknown.
