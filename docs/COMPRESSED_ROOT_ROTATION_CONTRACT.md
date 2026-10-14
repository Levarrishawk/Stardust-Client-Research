# Compressed absolute root rotation

`CompressedRootRotation.Create(ReadOnlySpan<byte>)` imports an owned CKAT/0001 top-level optional QCHN. It exposes `SourceSha256`, authored `FrameRate`/`EndFrame`, `HasRotation`, three selector bytes, immutable raw `CompressedRotationKey` values and a repair count. `SampleRotation(double frame)` returns a value quaternion and per-sample repair diagnostic. No borrowed source buffers, mutable interval cache, engine state or teardown resources are retained.

## Compatibility boundary

Exactly zero or one top-level QCHN chunk is permitted. A FORM QCHN, duplicate, wrong version, out-of-order suffix or malformed IFF extent rejects through the frozen structure decoder or this importer. Nested AROT/QCHN is unrelated: a nested-only clip has `HasRotation=false`, empty keys and selectors zero; `SampleRotation` rejects absence rather than inventing identity. The importer validates the ordinary CKAT envelope and channel structure with the existing decoder. MSGS/LOCT suffix content is not evaluated or newly validated here; importing a combined clip makes no combined-transform claim. Existing `CompressedLocomotion` remains unchanged and rejects root QCHN.

QCHN is U16 little-endian key count, three U8 selectors, and exactly count records of U16 frame followed by U32 packed bits. Width is `5+6*count`, count is at least two, times strictly increase and remain within the positive INFO end frame. The enclosing frozen decoder applies an 8 MiB byte/100000 node/depth16 budget, positive frame rate <=1000 and signed INFO count/index validation. U16 count is bounded by its representation and increasing end-bounded keys; malformed enormous counts fail before key allocation. Selectors must be in deployed range128..254. U32 bits are preserved, even if interpreted as IEEE floats they would be NaN; they are not floats.

[Counted layout](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-23_CKAT_V0001_COUNTS_AND_QCHN.md) and [placement route](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-23_CKAT_QCHN_RUNTIME_ROUTE.md) establish the optional top-level field at template+0x60 and common reader00824870. [Root mode/evaluator evidence](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-23_CKAT_ROOT_MOTION_OPTIONALS.md) establishes use of the same quaternion evaluator00824F00 as joint rotations. These prior observations are Confirmed; no new executable body was read. [Combined transform order](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-24_CKAT_COMBINED_ROOT_TRANSFORM_ORDER.md) remains a separate boundary.

## Numerical policy

Each key uses frozen [PackedQuaternionDecoder](PACKED_QUATERNION_DECODE_CONTRACT.md). Negative radicands retain a diagnostic and clamp before finite normalization. The original ordinary animated path can propagate NaN; this preview policy deliberately differs. Exact keys return prepared publications. Interior sampling uses float interval fraction and normalized shortest-path System.Numerics Quaternion.Slerp, matching the existing [joint preview policy](COMPRESSED_ANIMATION_SAMPLING_CONTRACT.md). Frames must be finite in [0,EndFrame]. Before the first and after the last key, within clip bounds, endpoint holds are explicit modern policy. Random/reverse access is stateless. Repairs report the selected key or both interval endpoints.

This provides an absolute quaternion curve in native component coordinates. It supplies no relative delta, looping, elapsed-time clock, coordinate conversion, LOCT coordinate-frame behavior, actor/root authority, gameplay motion, x87 bit equivalence or original interpolation-threshold parity.

## Verification and reproduction

The independent Python peer constructs fixtures and expected F32 packed components/double interpolation before any native build. Five synthetic rotating tracks cover identity, 180-degree turn, negative-dot antipodal interpolation, established overshoot repair and preserved `FFFFFFFF` raw bits; absent and nested-only fixtures cover placement. Ownership tests mutate original byte arrays, attempt key-list writes and alter returned value copies. Twenty-two malformed inputs and finite/range sampling failures reject. Native scenarios also prove the old locomotion decoder still rejects every selected root track.

Exactly two original payloads are obtained by raw-name CRC-checked TRE indices under the existing effective overlay/configuration, without decoding other animations:

| Clip | Priority | Bytes | QCHN keys | End | SHA-256 |
| --- | ---: | ---: | ---: | ---: | --- |
| appearance/animation/rabbit_std_turn_right.ans |30|7520|12|30|87A9704BE53F7FBEA888864FF6A526C4DCFC582B8B4ABBA19E736D2BB8CC1B55 |
| appearance/animation/all_b_cbt_hop_turn_right_90.ans |30|5280|11|35|0139B7B7547466E69BF87928C9C3B59F17F259A005F9023717E11347B926E79F |

The first is rotation-only, the second also contains LOCT. Selection comes from the existing private effective appearance inventory and durable combined-clip evidence, not a new32/7771 corpus audit. Both source hashes are pinned in the peer. Baseline EXE SHA-256 is E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150, configuration SHA-256 is22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524.

Final native242 checks and22 malformed fixtures pass. Independent comparison covers56 quaternion samples, including16 queries across the two originals, max component error5.651946144435982e-8 at tolerance2e-6, comparing quaternion signs equivalently. Targeted and default .NET builds have zero warnings/errors; default smoke has18 events/zero failures. No original executable execution or prototype host rendering occurred.

Portable runner (external historical reference) requires explicit Python, cached NuGet packages, original EXE, asset root and config paths. It generates expected fixtures before offline native build and verification, retains outputs only in a fresh ignored local-private descendant and runs default core smoke. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R71_ROOT_ROTATION.md).

Next action: implement and evaluate an explicitly scoped relative-rotation/combined-track evaluator against the durable recovered formula q_relative=q1*conj(q0), the q1-derived matrix and scalar translation write order, using independent fixtures. External actor basis and application remain Unknown; absolute sampling alone does not implement that boundary.

Lead review amendment: Create rejects input exceeding8 MiB before source.ToArray(), preserving the frozen parser limit. One oversized-source negative adds a242nd native check (22 malformed byte fixtures remain separate). Source inspection confirms admission precedes copy and decode; no allocation instrumentation or reflection was used. Focused offline suite and default smoke pass after this change.
