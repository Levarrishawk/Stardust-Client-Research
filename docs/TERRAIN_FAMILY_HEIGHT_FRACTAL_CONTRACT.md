# Explicit family-routed height-fractal contract

Round 51 adds one bounded, stateless engine-neutral route in [TerrainFamilyHeightFractal.cs](../../SWG-Godot-Client/poc/Stardust.Core/Geometry/TerrainFamilyHeightFractal.cs). It composes the accepted owned family set, explicit supported-mode sampler factory and retained AHFR consumer. The route is a modern compatibility slice; original active precision controls, cache policy and whole-terrain parity remain Unknown.

## Inputs and strict eligibility

`Evaluate` accepts an already owned `TerrainFractalFamilySet`, encoded AHFR bytes, caller cache shape/cell, finite F32 coordinate/initial-height/influence bits, independent table/construction/query/consumer precision controls, limits and cancellation. Every precision is explicitly 24, 53 or 64; there are no defaults for arithmetic controls.

Only typed AHFR/0003 with a known IHDR, enabled header, operations 0..3 and finite scale is operational. Influence must be strictly positive and finite. Disabled rules, nonpositive influence, malformed or opaque inputs and unsupported operations reject; this route does not perform the broader consumer's early-skip behavior. The rule's actual `Fields.FamilyKeyBits` is converted with `unchecked((int)...)` and resolved by exact `Find`. There is no missing-family fallback. Every signed ID, including -1, can succeed when the owned modern set contains it; a missing -1 rejects. This is not an inference about an original pointer sentinel.

Before sampler construction, the selected raw entry must have modes 0..5, both optional curve flags clear, octave count 1..16, seed other than Int32.MaxValue and finite used frequency/amplitude/coordinate-scale/offset fields. Disabled curve parameter words remain uninterpreted. Optional sine is explicitly off. The accepted sampler retains its additional normalization, gradient and numeric eligibility checks; these can reject during construction or query.

Caller dimensions are 1..128 with at most 1024 cells; column and row must be inside the exact shape. This route creates a fresh local sampler, queries that cell once and requires `ConditionalComputedMiss`. The sampler's owned lifetime is scoped by `using`, including consumer failure and cancellation. Each invocation constructs independently, so repeated calls return fresh misses. There is no persistent cache, reload, FFRA, layer traversal or active-control recovery.

## Arithmetic and receipt

The returned retained sample flows directly into `TerrainSuppliedHeightFractal.Evaluate`; the receipt retains the identical sample object and requires `Applied`. No projected F32 sample is injected into that consumer. With rounding R at explicit consumer precision, target T=R(sample*scale), height H and influence W, operation 0 computes R(H+R(R(T-H)*W)), operation 1 R(H+R(T*W)), operation 2 R(H-R(T*W)), and operation 3 R(H+R(R(R(T*H)-H)*W)). The accepted consumer performs its final F32 store and rejects nonfinite output.

The immutable receipt carries source SHA-256/input size, exact owned family entry and raw input, owned decoded rule, caller request and controls, computed-miss origin, retained sample, actual consumer result and logical accounting. Mutating caller buffers or exported copies cannot change it. Shared read-only family snapshots can be used by callers on different threads because each call creates and disposes its own thread-owned sampler. No original client thread or failure semantics are asserted.

## Aggregate limits

All caller controls, dimensions, scalar operands, rule-size cap and complete conservative logical route caps are checked before rule decoding or sampler construction. Limits allow rule bytes 0..65536 and logical bytes/work 0..4194304. A cap below the required amount rejects. The decoder's own minimum usable input limit remains 24.

For rule length L and cell count C, accounted bytes are `8*L+65536+9512+136+32*C+160+16*80+256`; accounted work is `L+20000+16*16+C+16*512+64+32`. These reserve worst-case 16-octave decode/table/cache/query/receipt payload allowances. Accounting excludes the caller's preexisting family snapshot, managed runtime overhead and unbounded unrelated process allocations. Fixed subordinate factory/query budgets are above the route's bounded requirements. Cancellation is checked before decode, before construction, through accepted construction/query and before returning; completed decoded data is not exposed on failure.

## Verification and confidence

[Native scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/TerrainFamilyHeightFractalScenarios.cs) and the [independent producer/verifier](../../SWG-Godot-Client/poc/round51-tests/Rendering/verify_route.py) compare 203 complete routes: 81 calls using the effective original first family under all four precision-control combinations, 90 authored synthetic calls across five signed IDs/six modes/three profiles, and 32 rejection cases. Synthetic coordinates, operands and raw variants are explicitly authored. The expected path computes table/noise/normalization and consumer arithmetic independently; no expected native sample is supplied to the route. Original executable bytes are hash-gated and read statically; it is never executed.

Ten ownership checks cover input/rule/export mutation, family/raw/sample identity, repeat fresh misses, stable prior receipts and calls from another thread. Frozen supported-mode/default-mode/kernel/raw/consumer suites, the lead 2592 family join and default smoke are the proportional regression gates. The fresh offline [runner](../../SWG-Godot-Client/poc/round51-tests/Rendering/Invoke-Route.ps1) accepts explicit private input roots and rejects nonprivate, existing or linked output ancestors before creating output.

**Confirmed:** the modern bounded family lookup-to-retained-affector route and its tested rejection/ownership behavior. **Strongly inferred:** compatibility of this finite arithmetic composition from the accepted executable-backed component evidence. **Unknown:** original active controls, runtime family/cache lifetime, exceptional behavior, optional helper modes and complete terrain parity. These measures add no function/protocol/research completion count.

The next probe is one explicitly controlled accepted-family filter/height composition with supplied traversal and cache policy. Keep original policy and broader layer recursion separate until evidence closes them.

## Round53 shared preparation

[Opt-in patch sampling](TERRAIN_SINGLE_SAMPLER_HEIGHT_PATCH_CONTRACT.md) shares
internal strict preparation/completion with this public route. Evaluate retains
its fresh-call behavior and independent accounting. Only opt-in patch receipts
carry SharedPreparationCell accounting; a computed miss is never relabeled from
a hit. Frozen203 direct-route cases and151 complete patch cases pass.


## Round54 accepted increment

The [selected original filter](TERRAIN_GENERATOR_FRACTAL_FILTER_RULE_CONTRACT.md) and [per-cell field](TERRAIN_FAMILY_HEIGHT_FIELD_CONTRACT.md) pass independent replays. Lead Version6 composes selected root AHFR and layer3 FFRA under explicit synthetic coordinates, prior heights and controls;400 native/headless/GPU cells, mixed/zero-state checks and late zero-visual-allocation rejection pass. Original layer admission, traversal and active controls remain Unknown.
