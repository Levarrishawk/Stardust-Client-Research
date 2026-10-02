# Skeletal blend descriptions

The owned SKMG0004 decoder retains BLTS target order and each BLT sparse-array order. INFO contains little-endian signed position/normal counts followed by one NUL-terminated ASCII variable name. POSN/NORM records contain a uint32 index and three float32 deltas (16 bytes); optional DOT3 has its own int32 count and the same records. Duplicate indices and names remain ordered. Optional terminal HPTS stays explicit metadata; its transforms are not implemented.

Counts, extents, finite bounded vectors, source-array indices, tag order and aggregate budgets are validated before publication. Missing arrays are accepted only with zero declared count. Descriptions derive from the already-owned geometry and introduce no archive reads. This bounded parser policy may reject malformed inputs tolerated by the executable.

## Evidence and compatibility

Executable loader 00800EF0 calls 007FAA00 for each BLT. The latter constructs `/shared_owner/` plus the serialized name at007FAA7F–007FAAC1 and reads ordered sparse arrays. Names therefore identify shared-owner variables without a serialized Boolean domain flag. Confirmed from baseline executable instructions and original asset bytes.

The original human body/head object chain contains30 targets/27 distinct variables,2,554 position,3,240 normal and3,520 tangent deltas; no HPTS target. All30 independently computed ordered record hashes match the native reader. The original twelve-source329,296-byte closure is unchanged.

Generator008031A0 applies position/normal deltas before skin transfer. Weight multiplier015E4DA0 is float32 0.003921568859368563; activation epsilon015E57F4 is0.008999999612569809. Tangent helper008036C0 uses the same selection;007F9F20 adds XYZ into16-byte tangent records without writing W. This is confirmed static evidence, not an executed original-client comparison. Application, global direction normalization and value-domain policies remain the next implementation question. Exact x87 rounding is unproven.

## Reproduction

Run native `--skeletal-blends` with the existing pinned object/SAT/configuration arguments, then `tools/python/verify_skeletal_descriptors.py` over its output. `verify_skeletal_blends.py` independently parses original bytes and hashes name, counts, ordered records and optional metadata. Synthetic fixtures cover truncation, invalid counts/index/vector/name, budgets, zero-target absence and input ownership. See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SKELETAL_BLEND_DESCRIPTIONS.md).

## Explicit shape evaluation

`SkeletalBlendShapeEvaluator.Apply` accepts exactly the description's case-sensitive shared-owner names, with explicit0..255 values. That interval is a bounded prototype policy; original variable factory clamping/defaults are not established. Every target and index validates even when inactive; unsupported HPTS rejects. Selection is copied, arrays are owned, cancellation precedes publication. Source topology, shader maps, UVs and weights remain identical references.

Ordered sparse XYZ additions precede weighted skinning. Float32 multiplier/epsilon above means0/1/2 inactive and3 active. If any target activates, all normals and tangent XYZ normalize, including untouched entries; lengths at most float32 9.999999747378752e-6 remain unchanged. Tangent W is preserved. Direct generator sites008033BB/008033F5 precede transfer00803450; normal00803555/00803586 and tangent00803857/00803885 establish global normalization. Normalizer00AB5C40 and constants015EAAF8/015DC6F8 establish the degenerate threshold and reciprocal unit value.

The modern policy uses float32 component multiply/add and normalization, without fused operations or an x87 bit-identity claim. An independent double reference from original bytes agrees on22,070 vectors under0/2/3/127/255 selections, maximum error2.1351779699863016e-7 with2e-5 tolerance. Synthetic noncommuting rotation/translation proves deltas precede skin transfer; global untouched-normal/zero preservation and inactive invalid-index gates pass. [Application checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SKELETAL_BLEND_APPLICATION.md). Animated live GPU integration is next.
