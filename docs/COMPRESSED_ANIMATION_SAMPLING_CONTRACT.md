# Compressed animation sampling

`CompressedKeyframeSampler` prepares an owned, engine-neutral view of a decoded CKAT/0001 clip. It copies transform/index/key data, decodes packed rotations once and retains repair counts. Sampling is deterministic and has no shared interval cache, so reverse/random access gives the same result as forward access.

## Bounded behavior

SampleFrame accepts finite frame positions from zero through EndFrame. SampleSeconds accepts finite seconds from zero through EndFrame/FrameRate and converts with the authored rate. Caller owns looping, speed, elapsed time and state transitions. Negative/out-of-range/nonfinite times reject. Endpoint holds before a channel's first key and after its last key are explicit modern preview policy; original before-first extrapolation is not claimed.

Static rotation/translation arrays are selected by XFIN flags/indices. Dynamic translations linearly interpolate adjacent F32 values. Dynamic rotations use normalized finite packed publications and shortest-path `System.Numerics.Quaternion.Slerp`, followed by normalization. Exact key samples return the prepared values. This preserves the recovered route while deliberately avoiding an x87/interpolation-threshold bit-equivalence claim. Optional event/root suffixes are exposed as unsupported metadata and never executed by this sampler.

Construction revalidates public descriptors: budgets, strictly increasing key times, finite translations, folded unique names, supported selectors and every transform index. Owned arrays survive mutation of caller arrays. Limits are the declared compressed-reader budgets. Returned samples own values and retain per-transform repair flags.

## Evidence and verification

The [translation route](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-23_CKAT_TRANSLATION_INTERPOLATION.md) confirms per-axis linear interpolation and final hold. The [rotation route](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-23_CKAT_PACKED_QUATERNION_EVALUATION.md) confirms adjacent packed decode, dot-selected interpolation and near-equal branches. Native hand-computed vectors cover a 180-degree keyed turn, intermediate quarter/half samples, linear translation, reverse access, seconds conversion, sparse endpoint holds, ownership and malformed descriptor/time rejection.

`--skeletal-sampling` uses the same pinned human object/SAT/configuration arguments as [compressed structure reproduction](COMPRESSED_KEYFRAME_STRUCTURE_CONTRACT.md). The optional source closure is unchanged: fifteen original sources, 726,134 bytes, selected four-second ambient clip at30 FPS/end120. Independent original-byte decoding plus double numerical interpolation verifies all38 transforms at frames0,0.5,15.25,30,59.5,90,119.5,120,15.25,0:380 samples, maximum error1.1568573421438799e-7 (tolerance2e-6), zero selected repairs. Source identities and every key remain independently audited.

Observed disk arrays and sampled agreement are Confirmed. Original higher-level time/event/priority/blending semantics remain separate. There is no rendered or timed playback claim here. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_COMPRESSED_ANIMATION_SAMPLING.md). Next: name mapping and local pose composition through the existing hierarchy/deformation boundary.
