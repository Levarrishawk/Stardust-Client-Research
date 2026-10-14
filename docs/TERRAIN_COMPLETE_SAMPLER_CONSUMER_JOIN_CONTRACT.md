# Complete conditional sampler to retained consumers

Round49, 2026-10-04. The lead native runner (external historical reference) composes the actual [raw MFRC decoder](TERRAIN_FRACTAL_RAW_INPUT_CONTRACT.md), [owned conditional mode0 sampler](TERRAIN_CONDITIONAL_FRACTAL_SAMPLER_CONTRACT.md), and frozen retained AHFR/FFRA/layer-influence consumers. Expected arithmetic comes from the independent Fraction peer (external historical reference). The original executable is SHA-gated and inspected statically; it is never executed.

## Inputs, stores and provenance

One pinned first MFRC family from the effective Tatooine overlay supplies its exact raw parameters. Its84-byte identity is85A84EFBDF4EE507C0011C9E4D4ED130EBF4719735A521FD491ABD286EBD723E. Those bytes and words remain private. A second frame changes only octave count and amplitude multiplier and is explicitly synthetic. Eight coordinate pairs, three independent table profiles, three construction/normalization profiles and three query profiles yield432 compositions:216 original-family-parameter cases and216 synthetic-family cases. All coordinates and selected arithmetic profiles are supplied modern inputs; original noise samples remain unavailable.

The independent normalization model follows SHA-gated013E6DD0 instructions: retained weight accumulation at013E6DF8, retained weight multiplication at013E6DFE, final sum F32 store013E6E06, reload/reciprocal013E6E0C and reciprocal F32 store013E6E12. It independently follows every coordinate, octave sum, frequency and amplitude store before the mode0 finish. The native runner generates its sample through the real sampler; expected magnitudes are not supplied to the native path.

Each case constructs a fresh explicitly bounded1x1 cache, queries the exact cell twice, and checks ConditionalComputedMiss followed by CacheReloadF32. Both actual samples pass through AHFR, FFRA, retained filter-query influence and a child AHFR. Source computation origin remains distinct from the nested supplied-wide carrier representation. There is no inferred request-wide clear, original family pointer lifetime, engine FPU default or legacy mutable setter sequence.

## Verification and compatibility limit

All432 exact compositions pass with zero-warning/error builds. Four retained results require the full unsigned64 extension. There are108 AHFR miss/hit differences and108 final height differences. This selected join has zero FFRA scaled-result differences; its exact comparisons pass, and earlier [supplied retained joins](TERRAIN_RETAINED_FRACTAL_CONSUMER_JOIN_CONTRACT.md) retain separate general FFRA witnesses. Counts describe these fixtures rather than a corpus percentage.

The native full sampler's own274 scenarios/5940 returned queries/1134 action rejections/792 miss-hit differences pass in the lead checkout. Failure atomicity, cancellation, owner affinity and bounded allocations remain the sampler's modern policies. Optional sine/curves, other modes, effective original controls, complete recursive terrain requests and original generated terrain parity are Unknown or excluded.

Reproduce by generating private fixtures with the peer's `generate` command, building SamplerConsumerJoin.csproj against actual Core with the offline cached dependencies, running its DLL with fixture/native-output paths, then using `verify` with the same original input paths. Fixture/native/receipt destinations must be ignored private paths. The [round checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R49_SAMPLER_AND_SOURCED_HOST.md) records gates and corrections. Next: own a bounded immutable family set and explicitly route sampled cells into a layer executor without manufacturing original request or control policy.
