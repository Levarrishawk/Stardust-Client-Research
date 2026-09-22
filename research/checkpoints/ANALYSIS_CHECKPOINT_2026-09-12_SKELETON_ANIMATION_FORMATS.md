# Analysis checkpoint: skeleton, logical animation and keyframe formats

Date: 2026-09-12

## Scope

Client loaders and runtime boundaries for `SLOD`, `SKTM`, `LATT`, `CKAT` and `KFAT`, using static disassembly of `Stardust.exe` and the previously sampled installed asset chain.

## Confirmed findings

### Skeleton templates are a typed factory family

The skeleton-template registry at `0x0082CCA0` maps outer tags to factories. `basic_skeleton_template_install_VA_0082A620` registers `SKTM`; `lod_skeleton_template_install_VA_00829EB0` registers `SLOD`.

`SLOD/0000` stores a signed 16-bit skeleton-template count in `INFO`, then reads that many nested skeleton templates through the common registry. Runtime detail lookup returns at most four levels even if the asset contains more, and the selected nested skeleton is retained/released through the template ownership API.

`SKTM` dispatches versions `0001` and `0002` to separate loaders (`0x0082B320` and `0x0082AC30`). The BasicSkeletonTemplate runtime owns multiple typed arrays/maps rather than one flat matrix list. The load path includes transform names and transform records; the v2 path is the installed reference format.

### Logical animation tables map names to typed child templates

`logical_animation_table_load_VA_007E0F20` accepts only `LATT/0000`. Its `INFO` record contains a base/associated resource string and a signed 16-bit animation-entry count. Each `ANIM` form contains:

1. an `INFO` string naming the logical animation;
2. a nested animation-template object loaded through the generic skeletal-animation-template registry.

Names are normalized/trimmed and entries are sorted for lookup. A distinguished fallback/default logical name is tracked; if the asset omits it, the client appends a synthesized null/default entry. This means logical animation lookup has defined fallback behavior and cannot be represented as an unchecked dictionary of clips.

### Two keyframe families coexist

The skeletal-animation-template registry separately registers:

- `CKAT` -> `CompressedKeyframeAnimationTemplate`;
- `KFAT` -> `KeyframeSkeletalAnimationTemplate`.

`CKAT` supports versions `0000` and `0001`; `KFAT` supports versions `0002` and `0003`. The installed sample corpus uses `CKAT/0001` and `KFAT/0003`, but compatibility loaders must retain the older accepted versions.

The effective loaders share the same conceptual channel envelope:

| Form/chunk | Confirmed structural role |
|---|---|
| `INFO` | clip duration/rate and channel-family counts |
| `XFRM` / `XFIN` | per-transform name plus channel/index/flag metadata |
| `AROT` | animated rotation key data |
| `SROT` | static rotation values |
| `ATRN` | animated translation key data |
| `STRN` | static translation values |
| `MSGS` / `INFO` | animation message count/metadata followed by message records |
| `LOCT` | locomotion translation channel |
| `LOCR` | locomotion rotation channel; present in `KFAT/0003` |

The first `CKAT/0001 INFO` value is 32-bit and the remaining six counts are signed 16-bit. `KFAT/0003 INFO` uses seven 32-bit values. This is a concrete wire-format difference; one shared packed C structure would decode them incorrectly.

The runtime chooses one of four evaluation functions according to whether locomotion translation and locomotion rotation channels exist. Rotation evaluation performs quaternion interpolation; translation evaluation uses time/value records and precomputed reciprocal intervals. Static and animated channels are stored separately and referenced by compact transform metadata.

### Runtime data must be transformed, not memory-mapped

Loaders allocate typed vectors, expand transform metadata, compute reciprocal time intervals and select evaluator function pointers after parsing. The file payload is therefore not the final runtime representation. A 64-bit replacement should parse into fixed-width disk records, validate indices/counts, then build pointer-free engine-neutral clips and per-joint channel bindings.

## Strong inferences

- The first 32-bit `INFO` field is a floating duration or frames-per-second value and the following values are channel counts. Their exact historical labels require correlation against representative decoded assets.
- `LOCT` and `LOCR` provide root/locomotion displacement separated from ordinary joint channels. The runtime evaluator split and naming make this strong, but exact coordinate conventions still require fixture validation.
- `SKTM` arrays encode transform hierarchy, bind/prebind rotations/translations and transform-name lookup. Exact v2 disk-field order remains partially open.

## Replacement requirements

1. Implement version-explicit readers: `SLOD/0000`, `SKTM/0001-0002`, `LATT/0000`, `CKAT/0000-0001`, and `KFAT/0002-0003`.
2. Keep every stored integer width independent from host pointer size.
3. Preserve logical-name fallback and sorted lookup semantics.
4. Decode static and animated rotation/translation channels separately and validate all transform indices.
5. Represent locomotion translation/rotation as explicit root-motion outputs.
6. Preserve animation messages/events and deterministic transition timing.
7. Convert coordinates/quaternions only at a documented importer boundary and verify against known poses.

## Confidence and remaining unknowns

| Claim | Confidence |
|---|---|
| accepted outer versions and factory separation | Confirmed |
| LATT ANIM name-to-template structure and fallback insertion | Confirmed |
| effective keyframe child tag sequence | Confirmed |
| CKAT/KFAT INFO width difference | Confirmed |
| four evaluator variants depend on locomotion channel presence | Confirmed structurally |
| exact INFO labels and coordinate conventions | Unknown |
| exact SKTM/0002 field order and bind-pose math | Partially reconstructed |
| animation state hierarchy/track-controller execution | Not covered by this checkpoint |

