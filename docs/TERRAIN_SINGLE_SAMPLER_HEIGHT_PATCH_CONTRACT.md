# Opt-in single-sampler family height patch

`TerrainFamilyHeightPatch.BuildSingleSampler` adds an explicit sampling policy to the [controlled height patch](TERRAIN_FAMILY_HEIGHT_PATCH_CONTRACT.md). Existing `Build` remains the frozen fresh 1x1-per-sample path, with identical prior request, coordinates, heights, bounds, receipt fields and logical accounting values. The new policy prepares the uniform rule/family once, constructs one fresh supported-mode sampler for the full rectangle, submits one row-major batch of unique cache cells and disposes the sampler before returning the immutable patch.

## Shared strict boundary and truthful provenance

The two family-route (external historical reference) internal helpers centralize preparation and retained-consumer completion. `Prepare` contains the unchanged typed AHFR/0003/known IHDR, enabled header, operations 0..3/finite scale, exact signed family lookup, modes 0..5/curves-disabled/octaves 1..16/seed/finite-used-fields checks. Public `Evaluate` preserves its preflight limits, fresh construction/query and returned independent-route accounting. Factory normalization/gradient and query numeric eligibility remain enforced by the accepted sampler. Optional sine stays explicitly off; no fallback or broader grammar is introduced.

`Complete` requires actual `ConditionalComputedMiss`, passes the actual retained sample object to the accepted height consumer and requires `Applied` with unchanged sample identity. Unexpected hits reject; they are never relabeled. The new builder shares the immutable decoded rule, family entry and raw parameters across all per-cell receipts. The owned encoded input snapshot and exported copies retain their existing ownership contract.

The patch has additive `SamplingPolicy` and `SamplingCounts` properties. Existing `Build` reports `FreshSamplerPerSample`; its receipts retain cache shape 1x1/cell(0,0), independent rule decodes and `IndependentRoute` accounting. The opt-in method reports `SingleFreshSamplerUniqueCells`. Every receipt truthfully records the full rectangle's width/rows and that sample's column/row. A fresh sampler cache starts empty, and each integer cell index is submitted once, including when adjacent coordinates lie inside the accepted hit epsilon or scaled coordinates coincide. Samples retain their actual computed-miss origin and wide-value/F32 projection; no cache-reload value is substituted into AHFR.

Returned opt-in counters describe one rule decode, one family lookup, one sampler construction, one sampler query batch, N computed cells, N height applications and one sampler disposal. Kernel octave-query count is N times the selected octave count. Default counters describe N independent preparations/constructions/batches/disposals. Construction/batch/application counters follow the actual opt-in call sites; default and octave counts follow the frozen route's fixed loops. These are deterministic operation counts for successfully returned patches, not profiler measurements. Failure returns no patch or counters.

## Coordinates, atomicity and lifetime

Both policies call the same request validator and stored binary24 axis helper. Dimensions, positive finite spacing/influence, finite scalar fields, sample caps, strict stored axis order, row-major ordering and finite display bounds are unchanged. Initial height and influence remain uniform. The new method changes sampling lifetime, not the F32 coordinate or retained AHFR graph.

All declared aggregate caps are checked before copying input, decoding/preparing the rule or constructing/querying the sampler. Cancellation checks remain before snapshots, during axes/request generation, in the accepted construction/query, during consumer completion and before publication. The `using` scope ends before patch construction. Query failure, late consumer overflow, display-bound failure or cancellation exposes no prefix and changes no prior result. Because the new query is a complete batch, all noise samples can be computed before a later AHFR/display rejection; atomic publication does not imply avoidance of that work. Precise mid-call cancellation timing is not claimed.

The sampler is private to one synchronous call and belongs to its calling thread. Returned numeric values and receipts remain valid after disposal. Repeated calls use new preparation/sampler state. Separate calling threads use separate samplers with a shared immutable family snapshot. There is no persistent cache, frame reuse, reload, original cache-policy or complete terrain-parity claim. Materials/normals/seams and prototype host hookup remain lead-owned.

## Logical reservations

Use the existing limits type and hard/default maxima. Let W/R be sample dimensions, N=W*R and L the encoded uniform-rule length. All octave-related allowances reserve the supported maximum 16 before rule decoding. The complete opt-in reservations are:

| Quantity | Formula |
| --- | --- |
| Copy bytes | `L+4*(W+R+N)+8*N+256+16*N` |
| Shared preparation bytes S | `8*L+65536+9512+136+32*N` |
| Per-cell receipt/query bytes B | `160+16*80+256 = 1696` |
| Shared preparation work T | `L+20000+16*16+N` |
| Per-cell query/consumer work C | `16*512+64+32 = 8288` |
| Aggregate route bytes | `S+N*B` |
| Total work | `L+32*(W+R)+T+N*(C+32)` |

The additional 16*N copy allowance owns the caller request array; extra 16*N work allows filling it. Shared 32*N bytes cover construction/cache and query candidate allowances. The sampler factory receives byte budget `9512+16*N+136` and work budget `20000+16*16`. Its single query receives `16*N+N*(160+16*80)` bytes and `N+N*(16*512+64)` work, bounded within its frozen hard caps at N<=1024.

`SharedPreparationBytes` and `SharedPreparationWork` are top-level opt-in costs. Each receipt's additive `AccountingPolicy=SharedPreparationCell` makes its 1696/8288 per-cell scope explicit. `AggregateRouteBytes` equals shared bytes plus the sum of receipt byte allowances. Default receipts remain `IndependentRoute`, with their previous complete independent-call bytes/work; default shared costs are zero. No shared table/decode cost is charged N times in opt-in receipts.

These are conservative logical payload/work reservations, not managed heap/RSS or elapsed time. Preowned family snapshots, runtime object overhead and caller export copies are excluded. Tight per-policy caps differ: opt-in owns an extra request array and avoids repeated preparations/tables. Fixture comparisons translate exact copy/route/work thresholds to the corresponding new policy, preserving the old thresholds verbatim for `Build`; they do not change any numeric input or independently expected sample.

Relative to default for the same rectangle/rule, total logical bytes decrease by `(N-1)*(8*L+65536+9512+136)-16*N`; work decreases by `(N-1)*(L+20000+16*16)-16*N`. Both are positive for the supported N>=4 domain. Querying the actual noise/consumer for every sample still occurs; the reduction comes from sharing preparation and table construction.

## Verification and evidence

The independent verifier (external historical reference) reuses frozen52's Fraction/finite kernel/mode/AHFR expectations. Neither stored heights nor expected retained samples are injected into native code. It verifies unchanged default outputs for all 151 cases/2211 heights, opt-in equivalent numeric axes/bounds/heights and retained values for every accepted frozen frame, and truthful per-cell shape/index/accounting provenance. Exact-cap fixtures are normalized per policy; malformed-input and hard-limit semantics remain independent.

Final results: 156 opt-in cases, 109 accepted/47 rejected, 2238 heights; 388 native ownership/identity/count/atomicity assertions plus the frozen 64 checks. The frozen151 cases include all 81 independent four-control profiles, six synthetic modes, default 256/hard 1024 sample boundaries and dimension 128. New focused fixtures include three near-neighbor precision profiles, late coordinate-floor query failure and zero normalization. Existing late AHFR overflow and finite display-height failure remain atomic. Sharing of one rule/family/raw object, distinct returned samples, read-only/export mutation, prior results after every failure, repeated calls and a separate calling thread are checked. Frozen 203 direct-route cases/32 rejects/10 ownership checks pass against the refactor.

Across 109 accepted opt-in patches, 2238 constructions/batches under the default policy become 109, with 2238 actual computed cells unchanged. Independently calculated logical savings are 161768192 bytes and 43306374 work units. Actual Core-reference builds have zero warnings/errors. Original EXE SHA `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150` gates the reused evidence/input chain; originals are read-only and never executed. Fixtures and original-derived family bytes/names remain ignored.

Reproduce with Invoke-Single.ps1 (external historical reference), explicit cached Python/.NET/package and read-only input roots, and a fresh private output directory. Native scenarios (external historical reference) compile against actual Core and the unchanged frozen52 scenarios. No shared test entrypoint/project or prototype host source is edited.

**Confirmed:** tested opt-in sharing, truthful miss/shape/identity, preserved frozen default results, immutable atomic behavior and deterministic logical/count reduction. **Strongly inferred:** finite component compatibility from the accepted executable-backed chain. **Unknown:** original traversal, active controls, cache/streaming/reload, optional curve/sine and whole terrain equivalence. Next bounded question: does a lead-owned switch of the existing family-height host to this explicit policy preserve GPU/surface/grounding output and teardown while exposing the same count reduction?


## Round54 accepted increment

The [selected original filter](TERRAIN_GENERATOR_FRACTAL_FILTER_RULE_CONTRACT.md) and [per-cell field](TERRAIN_FAMILY_HEIGHT_FIELD_CONTRACT.md) pass independent replays. Lead Version6 composes selected root AHFR and layer3 FFRA under explicit synthetic coordinates, prior heights and controls;400 native/headless/GPU cells, mixed/zero-state checks and late zero-visual-allocation rejection pass. Original layer admission, traversal and active controls remain Unknown.
