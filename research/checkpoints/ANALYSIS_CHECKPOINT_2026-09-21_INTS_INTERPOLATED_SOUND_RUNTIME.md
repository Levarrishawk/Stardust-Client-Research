# `INTS` interpolated-sound runtime and six-float contract

Scope: trace `ClientDataFile +0x80` ENGS entries into the live interpolated-sound controller and resolve all six INTS floats.

## Resolved payload

Each `INTS/INFO` record is:

```text
NUL-terminated attachment/customization lookup name
NUL-terminated sound-template path
float32 volume_at_input_0
float32 volume_at_input_1
float32 pitch_at_input_0
float32 pitch_at_input_1
float32 input_rise_rate
float32 input_fall_rate
```

The runtime input is normalized to the `[0,1]` interval before these curves are evaluated.

## Evidence chain

`ClientDataFile_getEngineSoundEntries_VA_0060AC00` returns the vector at `ClientDataFile +0x80`. Ship construction at `0x006ED5D0` creates one runtime interpolated-sound controller per 48-byte entry and copies the complete record through `0x00711C80`.

The per-frame controller at `0x00712030`:

1. moves its current normalized input upward toward the requested input by `deltaTime * value[4]`;
2. moves it downward by `deltaTime * value[5]`;
3. starts/resolves the authored sound when enabled and not already playing;
4. computes `volume = lerp(value[0], value[1], currentInput)` and clamps it to `[0,1]`;
5. computes `pitch = lerp(value[2], value[3], currentInput)`;
6. applies pitch and volume to the existing sound instance without restarting it.

The asset tuples independently confirm the labels. Normal engine records commonly use `0.7, 1.0, -16.0, 0.0, 0.3, 0.3`: volume increases from 0.7 to 1.0 while pitch rises from -16 to 0 as the normalized engine input increases, with symmetric rise/fall smoothing. Damaged loops use lower volume endpoints.

When disabled, an active sound is faded/stopped using a duration derived from the fall rate. Lookup of the attachment/customization name walks the owner/parent chain when needed, then starts the sound template at the resolved object.

## ENGS selection

`0x006ED130` compares the controlling normalized damage/state value against each entry's externally associated threshold and marks exactly the selected runtime entry enabled. Each enabled entry then performs its own continuous volume/pitch interpolation from the six INTS floats. Discrete damage-loop choice and continuous engine input are therefore separate axes.

## Replacement-client contract

- Preserve both strings and all six little-endian floats.
- Maintain one reusable sound voice per active entry; update parameters rather than restart every frame.
- Smooth normalized input with separate rise and fall rates.
- Linearly interpolate volume and pitch endpoints; clamp only volume to `[0,1]` as the legacy path does.
- Resolve the named attachment/customization target through the owning object hierarchy.
- Fade/stop cleanly when an entry becomes disabled or the owner leaves the scene.

## Stable provisional names

| Address | Name | Confidence |
|---:|---|---|
| `0x0060AC00` | `ClientDataFile_getEngineSoundEntries_VA` | Confirmed |
| `0x00712030` | `InterpolatedSound_update_VA` | Confirmed |
| `0x00712300` | `InterpolatedSound_setEnabled_VA` | Confirmed |
| `0x006ED130` | `ShipObject_selectEngineDamageSoundEntry_VA` | Strongly inferred |

## Confidence

- Six float meanings, interpolation equations, and distinct rise/fall smoothing: **confirmed by code and asset values**.
- Sound reuse and attachment lookup behavior: **confirmed by code**.
- Exact original public member names and pitch units: **unknown**. Values are passed unchanged to the legacy audio pitch setter; the replacement audio adapter must calibrate equivalent audible behavior.
