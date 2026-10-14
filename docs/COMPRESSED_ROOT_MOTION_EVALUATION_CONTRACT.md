# Compressed root-motion computation

`CompressedRootMotion.Create(source, CancellationToken)` owns CKAT/0001 root tracks and exposes `SourceSha256`, authored frame rate/end/speed, immutable `Positions`, the immutable frozen `RotationTrack` and `Mode`. `SampleDelta(fromFrame,toFrame,token)` returns a value quaternion, translation triple and repair flag. It has no actor, engine, clock, message scheduler or world-state reference.

## Four modes and recovered algebra

| Mode | QCHN | LOCT | Output |
| --- | --- | --- | --- |
| None | absent | absent | identity quaternion, zero translation |
| Rotation | present | absent | q1 * conjugate(q0), zero translation |
| Translation | absent | present | identity quaternion, p1-p0 |
| Combined | present | present | q1 * conjugate(q0), recorded q1 matrix action below |

The [root-mode evidence](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-23_CKAT_ROOT_MOTION_OPTIONALS.md) confirms dispatch at00825710 and routines00826660/008266B0/00826930/008267A0. [Recovered combined order](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-24_CKAT_COMBINED_ROOT_TRANSFORM_ORDER.md) confirms relative quaternion multiplication and scalar output order. This round implements that existing conclusion; it does not rediscover executable bodies.

For d=p1-p0 and canonical column-vector quaternion matrix M(q1), the ordered outputs are:

```text
x = (M[0,2]*d.x + M[1,2]*d.y) + M[2,2]*d.z
 y = (M[0,1]*d.x + M[1,1]*d.y) + M[2,1]*d.z
 z = (M[0,0]*d.x + M[1,0]*d.y) + M[2,0]*d.z
```

This is component reversal after transpose(M(q1))*d. The implementation spells out the canonical scalar-first column-matrix coefficients using Quaternion's X/Y/Z/W values; it does not substitute System.Numerics row-matrix storage. Prior [coordinate evidence](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_GODOT_COORDINATE_ADAPTER.md) and existing independently authored skeleton matrix fixtures establish the canonical quaternion action. The Python oracle instead uses conjugate(q1)*pureVector(d)*q1 then reverses the vector components. Asymmetric fixtures explicitly discriminate q0, qRelative and unreversed outputs. Reverse-time combined translation is recomputed with its new q1; it is not generally the negation of a forward result.

Computational order is Confirmed from prior evidence. Conventional matrix shorthand is Strongly inferred; external actor basis/application and normal gameplay reachability remain Unknown. No delta is applied to actor/gameplay state.

## Ownership, bounds and numerical policy

Cancellation and the8 MiB input guard precede copying. Frozen CKAT parser limits/version/index/suffix-order validation and [absolute QCHN importer](COMPRESSED_ROOT_ROTATION_CONTRACT.md) are reused unchanged. Top-level LOCT must be a chunk, optional at most once; FORM/nested conflation and duplicates reject. Its F32 speed/S16 count/14-byte U16-frame+F32XYZ records require exact width, count>=2, finite nonnegative speed<=1e6, finite per-axis positions within1e6 and strictly increasing times within EndFrame. MSGS is retained by the frozen tree but not executed or newly semantically validated. Raw QCHN selectors/U32 keys remain owned through RotationTrack. Public positions are copied read-only values; source identity is immutable SHA-256. Source bytes/parser trees are not retained.

Both sample frames must be finite in[0,EndFrame], including None mode; reverse/equal times are allowed. LOCT linearly samples stored F32 positions with a F32 fraction, holding endpoints within clip bounds. QCHN uses frozen finite repaired publication, endpoint holds and normalized shortest-path interpolation. Relative multiplication is normalized; matrix coefficients and scalar sums use double intermediates then F32 outputs. These are explicit modern numerical policies, not x87/matrix-rounding/interpolation-threshold bit equivalence or original NaN propagation. Repair flags combine sampled rotation endpoints. Cancellation is cooperative at entry, parser phase boundaries, every256 translation keys and final publication, not preemption inside frozen parsers. There is no shared interval cache or mutable state to roll back.

## Verification

The independent Python fixture runs before native build. Eight tracks cover all four modes, negative-dot/antipodal and repaired rotations, plus the same two original71 turn clips. Each has eight forward/reverse/equal/interior/endpoint pairs:64 deltas, including16 original queries. Forty-four malformed inputs cover both inherited QCHN/envelope cases and LOCT truncation/count/time/tag/finite/extent failures; sample ranges, canceled construction/sampling, oversized input and ownership are separate native checks. Owned raw original LOCT bits, including negative zero, compare exactly after decoding JSON to binary32.

Originals are priority30 rabbit_std_turn_right.ans (SHA87A9704BE53F7FBEA888864FF6A526C4DCFC582B8B4ABBA19E736D2BB8CC1B55) and all_b_cbt_hop_turn_right_90.ans (SHA0139B7B7547466E69BF87928C9C3B59F17F259A005F9023717E11347B926E79F). Exactly those two payloads are expanded from CRC-checked effective TRE indices; no32/7771 corpus scan. EXE/config identity gates and numerical decode fixtures reuse the sealed71 independent peer. No fresh executable body or original executable execution occurs.

Final native351 checks/44 malformed fixtures pass.64 independent comparisons have maximum quaternion error8.656571692799986e-8 and translation error7.926113869682894e-7. Quaternion tolerance is3e-6 sign-equivalent component error; translation tolerance is3e-6 times max(1,largest expected component). Targeted/default offline .NET builds have zero warnings/errors; default smoke passes18 events/zero failures. No Godot/rendered/live-server claim.

[Portable runner](../../SWG-Godot-Client/poc/round72-tests/Assets/Invoke-SelectedRule.ps1) requires Python, cached packages, original EXE, asset root and config; generates expectations before offline native and independent comparison. Fresh ignored private output retains detailed fixtures/logs. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R72_ROOT_MOTION_EVALUATION.md). Next: investigate the external actor basis/application boundary with an explicitly bounded evidence question before integrating these deltas into gameplay.
