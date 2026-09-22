# Analysis checkpoint: sound templates and audio resource boundary

Date: 2026-09-12

## Scope

Static reconstruction of the `SD2D`/`SD3D` `.snd` template family in `Stardust.exe`. This checkpoint covers factory registration, deployed format versions, sample ownership, the serialized field envelope, and the boundary between data-driven sound templates and the Miles-backed playback layer. It does not claim semantic names for fields whose consumers have not yet established them.

## Confirmed findings

### Two typed sound-template factories share one base layout

`sound2d_template_install_VA_0041C990` registers tag `SD2D` with factory `0x0041C920`. `sound3d_template_install_VA_0041C500` registers `SD3D` with factory `0x0041C490`. Both allocate a 0x90-byte object. The 3D constructor first builds the common Sound2dTemplate layout and then replaces the vtable with `0x015DC92C`; the 2D vtable is `0x015DC9DC`.

The common SoundTemplate base owns a vector of retained sample-resource/name objects at object `+0x04` and a template-name/path object at `+0x0C`. Destruction releases every retained sample before freeing the vector. This is real ownership, not a list of borrowed C strings.

### Exact accepted version envelope

`sound2d_template_load_VA_0041CFD0` opens `SD2D` and dispatches exactly four versions:

| Version | Reader |
|---|---:|
| `0000` | `0x0041D160` |
| `0001` | `0x0041D490` |
| `0002` | `0x0041D7F0` |
| `0003` | `0x0041DB30` |

An unknown version reaches the client's unsupported-tag diagnostic rather than being interpreted as the newest schema.

`sound3d_template_load_VA_0041C540` first invokes the common SD2D reader and then dispatches the enclosing `SD3D` tail:

| Version | Reader | Confirmed payload |
|---|---:|---|
| `0000` | `0x0041C630` | two raw 32-bit values; the first is stored at `+0x8C` |
| `0001` | `0x0041C680` | empty version chunk in this client schema |

After either tail, the runtime type field at `+0x88` is forced to `2` (3D). The deployed v1 wrapper therefore obtains its substantive parameters from the nested/common SD2D record; it is not a second independent descriptor.

### Deployed `SD2D/0003` wire layout

The v3 reader consumes:

1. a little-endian 32-bit sample count;
2. that many bounded strings (maximum read buffer 0x104 bytes), each passed through the sample-retain routine at `0x0041BAE0`;
3. twenty-eight raw 32-bit fields, stored in this exact order:

```text
+1C +20 +24 +28 +2C +30 +34 +38 +3C +40 +44 +48 +4C +50 +54 +58
+70 +74 +78 +7C +80
+5C +60 +64 +68 +6C
+84 +8C
```

The writer `sound2d_template_write_0003_VA_0041DE60` emits the same order and canonicalizes output to `SD2D/0003`. This independently confirms the parser layout and distinguishes it from incidental memory initialization.

The vtable exposes typed accessors for every member. Floating-point use is confirmed for `+1C,+20,+24,+28,+34,+38,+3C,+40,+60,+64,+68,+6C,+74,+78,+7C,+80,+8C`; the remaining fields are returned or compared as 32-bit integral/enum values. Exact labels such as gain, pitch, loop, category and priority remain provisional until playback consumers establish them.

### Post-load normalization is behavioral

The common loader applies policy after every version reader:

- if field `+0x44` is zero, `+0x84` becomes 5;
- if field `+0x44` is nine, `+0x84` becomes 3;
- a non-negative `+0x8C` sets `+0x88` to 1;
- when virtual predicate slot `+0x90` is true and `+0x8C` is non-positive, `+0x8C` is clamped to approximately `0.01f` (`0x3C23D70A`).

This normalization must be represented in a compatible importer/runtime. Merely decoding the 28 values is insufficient.

### Sample-set behavior is first-class

Template equality compares the retained sample vector as well as all common fields. Sample insertion enforces bounded path lengths and contains count/version compatibility checks before retaining the referenced resource. The deployed descriptor evidence already includes both one-sample music and multi-sample 3D effects. A replacement asset model therefore needs an ordered sample set plus selection policy; flattening `.snd` to one audio filename would lose client behavior.

## Strongly inferred findings

- `+0x88` is a runtime sound dimensionality/mode discriminator: load normalization assigns 1 and SD3D overrides it to 2.
- `+0x8C` is a distance/radius-like float participating in the 2D/3D classification boundary. Its exact public engine-era name is not yet proven.
- Several paired float members are ranges used to randomize or interpolate gain, pitch, timing, or attenuation. The pairing is structurally apparent, but assigning individual names without following Miles call sites would overstate the evidence.

## Replacement-client implications

The modern client should parse `.snd` into an engine-neutral descriptor containing the retained sample list, all versioned scalar fields, the normalized runtime mode, and the original source version. A separate audio adapter should translate that descriptor into the chosen modern audio backend. Miles object layouts and handles must not leak into the asset representation.

Compatibility requires accepting all four SD2D versions and both SD3D tails even if the installed corpus primarily exercises v3/v1. Conversion caches may store a modern normalized form, but the original `.snd` remains the authoritative source.

## Unknowns

- authoritative semantic names and units for the still-unlabeled subset of the 28 v3 fields;
- exact names for every legacy sample-selection and envelope mode;
- exact pitch unit and the remaining 3D-distance endpoint label;
- whether any title-specific audio message handlers bypass the ordinary template path.

## Confidence

- Factory tags, object size, accepted versions, v3 field order, types inferred from accessors, writer symmetry and normalization: **confirmed by code**.
- Sample-set requirement and common-base/3D-wrapper architecture: **confirmed by code and installed descriptors**.
- Runtime role of `+0x88`: **strongly inferred**.
- Individual semantic field labels: **unknown/tentative; deliberately not assigned**.

## Resume point

This original format-boundary checkpoint has been continued by `ANALYSIS_CHECKPOINT_2026-09-12_AUDIO_PLAYBACK_BRIDGE.md`. That continuation closes sample selection, volume/pitch/loop/distance consumers, backend calls, effective corpus shapes and the resident-versus-streamed 2D cutoff. Resume by inventorying scalar-value distributions and resolving the remaining mode labels; do not redo the closed bridge trace.
