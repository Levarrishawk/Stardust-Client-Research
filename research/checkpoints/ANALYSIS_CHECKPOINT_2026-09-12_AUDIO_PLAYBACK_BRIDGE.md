# Analysis checkpoint: audio playback bridge and dynamic parameters

Date: 2026-09-12

## Scope

This block follows `SD2D`/`SD3D` template accessors into concrete `Sound2d`/`Sound3d` runtime objects and the Miles Sound System API. It assigns semantic labels only where the runtime calculation and API destination agree.

## Confirmed runtime classes

MSVC RTTI resolves:

- `Sound2d` to vtable `0x015DC79C`;
- `Sound3d` to vtable `0x015DC72C`.

`Sound3d` derives through the common Sound2d initialization path and overrides the small set of spatial/type behaviors. The common object is large (at least `0x4B4` bytes) because it owns playback state, sample selection, attachment/spatial state, fade/envelope values, and a type-independent audio-handle wrapper. This layout is legacy-only and must not become a replacement ABI.

The common constructor at `0x00418F40` initializes dynamic parameter fields, queries the template for a sample-selection range/mode, and prepares a shuffled/order vector when required. `0x0041A2E0` selects among multiple samples according to a three-mode policy: random, sequential wrap, or a non-repeating shuffled/randomized order. Thus ordered sample sets are not interchangeable with a single resolved file.

## Type-aware Miles adapter

The client wraps 2D sample, 3D sample, and stream handles behind one logical handle. The local adapter resolves the active handle kind before calling Miles:

| Client adapter | Miles destination |
|---:|---|
| `0x0040FC40` | `AIL_set_sample_volume_levels`, `AIL_set_3D_sample_volume`, or `AIL_set_stream_volume_levels`; resets reverb/effects contribution on the same handle |
| `0x0040FE70` | `AIL_set_sample_playback_rate`, `AIL_set_3D_sample_playback_rate`, or `AIL_set_stream_playback_rate`; clamps computed rate to 5,000..60,000 Hz |
| `0x0040FBD0` | `AIL_set_3D_sample_obstruction`, clamped to 0..1 |
| `0x0040FB60` | `AIL_set_3D_sample_occlusion`, clamped to 0..1 |

Per-frame Sound2d update at `0x004196C0` computes effective volume and playback-rate multiplier, sends both through these adapters, and—when template mode is 2—also sends current obstruction and occlusion. The same logical update path therefore supports ordinary samples, 3D samples, and streams without exposing three handle types to game callers.

## Template fields now semantically constrained

The accessor table and dynamic runtime provide the following evidence-backed groups.

### Volume evolution (`+0x70..+0x80`)

Runtime initialization/update at `0x0041A7D0` and `0x00419B20` proves:

| SD2D offset | Confirmed broad role |
|---:|---|
| `+0x70` | volume evolution/selection mode enum |
| `+0x74` | interval before choosing a new volume target in the continuously varying mode |
| `+0x78` | lower volume target/range endpoint |
| `+0x7C` | upper volume target/range endpoint |
| `+0x80` | time used to interpolate between volume targets |

The selected/current volume is stored at Sound runtime `+0x478`, constrained to 0..1, and folded into effective volume by `0x0041AB50`. That result is passed directly to `0x0040FC40` and ultimately to the appropriate Miles volume setter. These are now implementation-grade semantic labels, although the original source member names remain unknown.

### Fade/envelope timing

`0x0041A5F0` and `0x0041A6D0` select two runtime durations from template range/mode fields. Effective-volume calculation at `0x00419D60` applies them at the beginning and end of the sample lifetime. Their broad roles are therefore fade-in and fade-out/envelope durations. Exact offset-to-original-name labeling for all involved mode/range fields remains pending because several special modes depend on sample iteration and total duration.

### Pitch/playback-rate evolution

`0x0041A940` selects a value from the template group beginning at `+0x5C`; `0x0041AAA0` combines the current pitch state, and `0x00419F00` converts it through an exponential transform into a playback-rate multiplier. The per-frame update multiplies that by the sample's base rate and calls `0x0040FE70`.

This confirms the broad pitch/rate role of the `+0x5C..+0x6C` family and its independence from the volume family. Follow-up closure proves the exact conversion is `pow(2.0, pitch / 12.0)`, so the unit is semitones. See `ANALYSIS_CHECKPOINT_2026-09-21_AUDIO_PITCH_AND_CATEGORY13.md`.

### Loop count and loop markers

Template integral fields `+0x2C/+0x30` are used as the endpoints of a per-instance integer selection stored at Sound runtime `+0x224`. Voice creation forwards that value to `AIL_set_sample_loop_count`, `AIL_set_3D_sample_loop_count`, or `AIL_set_stream_loop_count` when looping is eligible. A selected value of `-1` is treated as the indefinite-loop case and becomes the Miles zero loop-count convention on eligible single-sample voices.

Loop-block start/end values do **not** come directly from another anonymous SD2D scalar pair in this path. They are queried from the selected sample resource/metadata (`0x00412A50`) and then sent to `AIL_set_sample_loop_block` or `AIL_set_stream_loop_block`. The 3D sample route exposes loop count but no loop-block setter in the observed setup path. This distinction prevents a replacement importer from incorrectly assigning `.snd` fields to embedded WAV/stream loop markers.

For 3D voices, setup also forwards a distance pair to `AIL_set_3D_sample_distances`. Template `+0x8C` participates directly, with a non-positive value replaced by `2.0f` before a derived companion distance is calculated. This upgrades `+0x8C` from merely “distance/radius-like” to a confirmed 3D-distance input; the precise original max/min label remains unresolved.

## Runtime smoothing and spatial state

Obstruction and occlusion live as separate normalized runtime values and are smoothly driven toward boolean/environment targets before reaching Miles. They are emitted only for the 3D template mode. Effective volume also multiplies template-driven volume by distance attenuation, fade envelope, and global/category/user factors before the backend call. Consequently a modern audio adapter must not treat the template range as the final gain.

The factor split is now bounded more precisely. Sound runtime `+0x440` is an externally set per-instance gain multiplied by `0x0041AB50`; `+0x444` is an externally set pitch offset added before `0x00419F00` performs its exponential rate conversion. Separately, `0x0040C780` reads a clamped **13-entry** category-volume table at `0x01907F34` and applies a global master scale to every category except index 8. The installation loop at `0x0040CBA0` proves the 0..12 width: all entries default to 1.0, while configuration overrides index 1 from `soundEffectVolume`, index 4 from `userInterfaceVolume`, index 8 from `backGroundMusicVolume`, and index 9 from `playerMusicVolume`. Category selection comes from template field `+0x44` through its accessor. This explains why post-load normalization keyed by `+0x44` affects other category-dependent policy. The meanings of the remaining category indices are still unknown.

The final backend volume is therefore a product of at least template-driven dynamic volume, lifetime envelope, spatial attenuation where applicable, per-instance gain, category gain, and master/global scaling. These factors should remain separate controls in a modern mixer.

## Effective installed sound corpus

Resolving all 48 `searchTree_00_*` archives from `swgemu_live.cfg` by deployed priority produced 7,242 live `.snd` winners and no tombstones or parse failures:

| Effective root/schema shape | Count |
|---|---:|
| `SD3D` containing common `SD2D/0003` plus `SD3D/0001` tail | 4,043 |
| ordinary `SD2D/0003` | 2,671 |
| `SD2D/0003` followed by an additional top-level empty `0001` chunk | 528 |

Thus every effective descriptor uses the common v3 payload, and every effective explicit SD3D wrapper uses the empty v1 tail. Chunk-size inventory confirms that the extra sibling `0001` on 528 SD2D records is also zero bytes. The legacy SD2D loader evidently accepts it, but its provenance and whether it is intentionally ignored remain unknown. A strict modern parser must reproduce accepted boundaries before rejecting trailing data.

The reproducible inventory is `sound_overlay_winners.csv`, generated by `sound_overlay_inventory.py`; it records logical path, winning priority/archive, live status and IFF tag path.

### Scalar-distribution validation

`sound_scalar_inventory.py` independently decoded the trailing 28 common v3 scalar words for all 7,242 effective descriptors and wrote the complete value/count table to `sound_scalar_distributions.csv`. This validates several runtime observations against deployed content:

- loop-count endpoints `+0x2C/+0x30` are overwhelmingly 1 (6,163/6,162 records) or -1 (838 each), with matching distributions except one record;
- volume-policy mode `+0x70` uses only 0..3, led by 0 (4,597) and 2 (2,570);
- pitch-policy mode `+0x5C` likewise uses only 0..3, led by 0 (4,431) and 2 (2,782);
- the reserved-looking integral field `+0x4C` is zero in every effective descriptor;
- distance input `+0x8C` has 59 values, led by 10.0 (1,586), 0.0 (1,315), 20.0 (644) and 8.0 (412).

The category result exposes a confirmed original boundary defect. Apart from two anomalous records whose trailing-word alignment/content needs individual inspection, `+0x44` uses values 0..10, 12 and 13. Value 13 occurs 558 times and is overwhelmingly voice/dialogue content. The direct accessor and ordinary volume bridge do not remap it; `0x0040C780` indexes past the 0..12 gain array into the first word of an adjacent STL-like audio container. A replacement must use an explicit bounded category-13 dialogue bus rather than reproduce this unstable read. See `ANALYSIS_CHECKPOINT_2026-09-21_AUDIO_PITCH_AND_CATEGORY13.md`.

## Replacement-client implications

The engine-neutral audio runtime should contain:

- multi-sample selection policy and per-instance sample index/order state;
- independent volume and pitch target generators;
- fade-in/fade-out envelope state;
- distance/category/user gain factors;
- smoothed obstruction and occlusion;
- one abstract voice interface implemented by resident 2D, spatial 3D, and streaming voices.

The original Miles handle union and object offsets are not portable architecture. The data-driven policies and observable timing are compatibility requirements.

## Resident-versus-streamed 2D policy

The remaining cache threshold is now closed by code rather than naming inference.

- `ConfigClientAudio::install` at `0x0041B640` reads the literal option `ClientAudio/maxCached2dSampleSize`, defaults it to 64, clamps it to 0..512, and shifts left by 10. The stored unit is therefore bytes, with a default of 64 KiB and maximum of 512 KiB.
- `0x0040CCB1` obtains that byte value and `0x0040CCB7` passes it to `0x00412DD0`. The latter enforces a second defensive clamp of 64 KiB..512 KiB in the audio runtime.
- `0x0040C210` obtains a selected sample's byte size through `0x0040F340`. For a stream-capable sound, size greater than the threshold enters the branch containing imported `AIL_open_stream` (`IAT 0x015DC4B8`). A positive size at or below the threshold instead enters the branch containing `AIL_allocate_sample_handle` and `AIL_init_sample` (`IAT 0x015DC59C/0x015DC598`).
- `0x0041BAE0` repeats the same size comparison when constructing/loading an individual sample resource and forwards a boolean policy flag to `0x0040D2D0`.

Thus `maxCached2dSampleSize` is specifically the resident-versus-streamed cutoff for eligible 2D sounds, not a general decoded-audio cache budget. The comparison is strict: `size > threshold` streams; `0 < size <= threshold` uses a resident 2D sample. The 3D branch is selected separately through the template's 3D capability and uses `AIL_allocate_3D_sample_handle`, so this threshold must not be applied indiscriminately to spatial voices.

A distinct `ClientAudio/maxSampleCount` value is read into `0x01908044`, then clamped by `0x00412D80` to 16..64. That is a count limit and is independent of the byte-size cutoff.

## Confidence

- **Confirmed by code:** concrete runtime RTTI/vtables, type-aware Miles adapter destinations, rate clamp, obstruction/occlusion clamps, three sample-selection modes, volume group offsets/roles, pitch group's broad role, and fade-envelope use.
- **Strongly inferred:** conventional names “fade-in” and “fade-out” for the two lifetime-edge durations; their mathematical behavior is confirmed.
- **Confirmed by code:** `maxCached2dSampleSize` units, clamps, strict comparison, and its selection of streamed versus resident 2D Miles paths; independent 16..64 `maxSampleCount` clamp.
- **Unknown:** original member names, the precise max/min label for `+0x8C`, and the full meaning of every legacy sample-selection enum value. Exact pitch units and the category-13 defect are closed by the 2026-09-21 follow-up.
- **Unknown compatibility edge:** the meaning of the extra top-level `0001` chunk in 528 effective SD2D records.

## Stable resume point

Next audio work should inventory effective installed `.snd` values against the newly identified volume, pitch, loop-count and 3D-distance policy groups, then finish names/semantics for the remaining category and lifetime-envelope modes.
