# Compressed keyframe structure boundary

`CompressedKeyframeDecoder` owns CKAT/0001 transform and channel descriptors. This stage preserves packed quaternion words and selector bytes; it does not decode rotations, interpolate keys, execute events or play animation.

## Confirmed disk structure

INFO is exactly sixteen bytes: little-endian F32 frame rate, then six signed I16 values: end frame, transform count, animated rotation count, static rotation count, animated translation count and static translation count. XFRM contains the counted XFIN records. Each record contains a NUL ASCII name, U8 animated-rotation flag, U16 rotation index, U8 translation mask and three U16 translation indices. Mask bits 8/16/32 select animated X/Y/Z translation channels; clear bits select static translations.

AROT contains QCHN records: U16 key count, three U8 compression selectors, then U16 frame/U32 packed quaternion pairs. SROT contains seven-byte selector/word records. ATRN contains CHNL records: U16 key count followed by U16 frame/F32 value pairs. STRN is a counted F32 array. These are disk records; internal objects need not reproduce legacy memory layouts.

Counted optional channel sections follow XFRM in AROT/SROT/ATRN/STRN order. Animated channels require at least two strictly increasing keys within the clip end. Transform indices must address the selected channel array. Optional MSGS/LOCT/QCHN suffix nodes are retained in order as owned, uninterpreted IFF nodes; their contents are not validated as executable event/root-motion behavior.

Evidence: [counted INFO/QCHN](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-23_CKAT_V0001_COUNTS_AND_QCHN.md), [transform indices](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-23_CKAT_XFIN_CHANNEL_INDEX_VALIDATION.md), [static rotations](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-23_CKAT_STATIC_ROTATION_ROUTE.md), [translation corpus](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-23_CKAT_ATRN_CORPUS_AUDIT.md), [key times](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-23_CKAT_KEY_TIME_INVARIANTS.md) and [end frame](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-23_CKAT_INFO_FRAME_COUNT.md). Layout and sampled source agreement are Confirmed. Future quaternion numerical interpretation remains a separate implementation gate.

## Modern rejection policy

Decode limits: 8 MiB, 100,000 IFF nodes and depth sixteen. Frame rate is finite, positive and at most 1,000. End frame is positive; transform count at most 2,048; individual channel counts at most 4,096; combined animated keys at most 262,144. Translation values must be finite with magnitude at most 1e6. Names must be unique under explicit ASCII folding. Unsupported version, widths, count mismatches, flags, indices, times and suffix order reject. These limits are modern bounded-reader policy, not claims about every original rejection path.

## Selected original result

Through object → SAT → body LAT → PXAT → `appearance/animation/all_b_idl_ambient_01.ans`, fifteen unchanged effective sources total 726,134 bytes, each read once. Selected clip priority 30, 15,671 bytes, SHA-256 `798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B`.

The clip has 30 FPS/end frame 120, 38 transforms, 30 animated and eight static rotation channels, two animated and 112 static translation channels, 2,217 rotation keys and 67 translation keys. No optional suffix is present. Canonical decoded hash: `E364FEBE1D1D849EB40D61FA6DCBA2B77C9C673343B31CA22D2604054AB21BD6`.

Thirty-seven transform names match the selected L0 body skeleton after ASCII folding; `rifle` does not. Observation is Confirmed; its attachment/animation-hierarchy role is Unknown at this boundary. A future binder must resolve that role or report the missing target explicitly. Structural acceptance must not imply all transforms can be applied to the body skeleton.

`--skeletal-compressed-clip` runs the pinned original probe. `verify_skeletal_descriptors.py` independently rereads original sources, parses the selected raw channel records, validates all indices/times and compares the complete canonical hash and name-match report. Native synthetic ownership, every truncated prefix and malformed rate/version/count/flag/index/time/value/tail cases pass. The Python compressed audit is intentionally limited to this selected suffix-free sample.

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_COMPRESSED_KEYFRAME_STRUCTURE.md). Next: numerical packed-quaternion fixtures and explicit clip-to-skeleton mapping before pose evaluation.


Follow-up: [packed quaternion numerical decoding](PACKED_QUATERNION_DECODE_CONTRACT.md) now passes the selected original value audit. Key interpolation and pose application remain separate.

Native final review also verifies omitted zero-count channel sections, ownership of opaque suffix payloads after clearing the input and rejection of duplicate/out-of-order suffixes. Synthetic opaque payload bytes do not claim valid original event schemas.

## Reproduce the bounded original audit

Run from the repository root with .NET 8 and Python. The original installed data must match the pinned configuration and sources; output stays ignored.

```powershell
$env:APPDATA = Join-Path $env:TEMP 'stardust-appdata'
dotnet run --no-restore --project poc/Stardust.Core.SmokeTests -- --skeletal-compressed-clip C:/Stardust-DEV --configuration C:/Stardust-DEV/swgemu_live.cfg --member object/creature/player/shared_human_male.iff --expected-sha256 E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243 --appearance-sha256 951FCE8C2FFC0EB318C751026D24D8C9BF17263D706EB3C93D015867EC801A9C > local-private/generated-evidence/human-appearance-2026-10-01/packed-quaternion-native.txt
python tools/python/verify_skeletal_descriptors.py C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/generated-evidence/human-appearance-2026-10-01/packed-quaternion-native.txt --configuration-sha256 22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524
```

The directory is created by the preceding extraction/descriptor work; create it before running these commands in a fresh workspace. Configure the documented local toolchain first if project dependencies have not been restored. The native output includes all source identities, the complete structure hash, unmatched-name diagnostic and numerical rows; the verifier reads both original archive bytes and the baseline executable tables without executing the original client.

Validated MSGS/translation-only LOCT ownership and sampling now have a separate [locomotion contract](COMPRESSED_LOCOMOTION_CONTRACT.md); raw structure retention remains distinct from root rotation and callback execution.
